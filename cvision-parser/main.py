from flask import Flask, request, jsonify
import os
import tempfile
import json
import requests

from parser_service import extract_text_from_pdf, call_openai_parser

app = Flask(__name__)

@app.route("/parse", methods=["POST"])
def parse():
    file = request.files.get("file")
    job_title = request.form.get("jobTitle")
    job_description = request.form.get("jobDescription")
    job_id = request.form.get("jobId")

    if not file or not job_description or not job_id:
        return jsonify({"error": "Missing file, job description, or jobId"}), 400

    temp_dir = tempfile.gettempdir()
    path = os.path.join(temp_dir, file.filename)
    file.save(path)

    try:
        # extract and parse
        cv_text = extract_text_from_pdf(path)
        structured_text = call_openai_parser(cv_text, job_title, job_description)

        # convert the structured string to JSON
        parsed = json.loads(structured_text)
        applicant = parsed["applicant"]

        # post to backend
        candidate_payload = {
            "jobId": int(job_id),
            "name": applicant.get("name"),
            "email": applicant["contact"].get("email"),
            "phone": applicant["contact"].get("phone"),
            "location": applicant["contact"].get("location"),
            "profileSummary": applicant.get("profile"),
            "matchScore": int(applicant.get("matchScore", 0)),
            "skills": applicant.get("skills", []),
            "strengths": applicant.get("strengths", []),
            "weaknesses": applicant.get("weaknesses", []),
            "analysisSummary": applicant.get("analysisSummary")
        }

        print("Structured JSON:", json.dumps(candidate_payload, indent=2))

        post_res = requests.post(
            "http://localhost:5000/api/candidates/profile",
            json=candidate_payload
        )

        print("POST response status:", post_res.status_code)
        print("POST response body:", post_res.text)

        if post_res.status_code != 200:
            return jsonify({"error": "Failed to save to backend", "details": post_res.text}), 500

        return jsonify({"status": "ok", "saved": candidate_payload})
    
    except Exception as e:
        print("🔥 ERROR during parsing:", e)
        return jsonify({"error": str(e)}), 500

if __name__ == "__main__":
    app.run(port=5002, debug=True)
