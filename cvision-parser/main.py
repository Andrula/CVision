from flask import Flask, request, jsonify
import os
import tempfile

from parser_service import extract_text_from_pdf, call_openai_parser

app = Flask(__name__)

@app.route("/parse", methods=["POST"])
def parse():
    file = request.files.get("file")
    job_title = request.form.get("jobTitle")
    job_description = request.form.get("jobDescription")

    if not file or not job_description:
        return jsonify({"error": "Missing file or job description"}), 400

    # Use a cross-platform temp file path
    temp_dir = tempfile.gettempdir()
    path = os.path.join(temp_dir, file.filename)
    file.save(path)

    try:
        cv_text = extract_text_from_pdf(path)
        structured = call_openai_parser(cv_text, job_title, job_description)
        return jsonify({"structured": structured})
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == "__main__":
    app.run(port=5002, debug=True)
