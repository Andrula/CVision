from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from openai import OpenAI
from dotenv import load_dotenv
from fpdf import FPDF
import tempfile
import os
import time
import json
import re

load_dotenv()
client = OpenAI(api_key=os.getenv("OPENAI_API_KEY"))

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

def create_temp_pdf(text: str) -> str:
    pdf = FPDF()
    pdf.add_page()
    pdf.set_auto_page_break(auto=True, margin=15)
    pdf.set_font("Arial", size=12)
    for line in text.splitlines():
        pdf.multi_cell(0, 10, line)
    path = tempfile.mktemp(suffix=".pdf")
    pdf.output(path)
    return path

async def upload_to_openai(upload_file: UploadFile, label: str, override_name: str) -> str:
    with tempfile.NamedTemporaryFile(delete=False, suffix=os.path.splitext(override_name)[1]) as tmp:
        tmp.write(await upload_file.read())
        tmp.flush()
        temp_path = tmp.name

    with open(temp_path, "rb") as f:
        file_obj = client.files.create(file=(override_name, f), purpose="user_data")
    os.unlink(temp_path)
    return file_obj.id

@app.post("/parse-cv/")
async def parse_cv(cv_file: UploadFile = File(...), job_file: UploadFile = File(...)):
    try:
        start = time.time()

        cv_file_id = await upload_to_openai(cv_file, "CV", "cv.pdf")

        job_text = (await job_file.read()).decode("utf-8")
        job_pdf_path = create_temp_pdf(job_text)
        with open(job_pdf_path, "rb") as f:
            job_file_obj = client.files.create(file=("job_description.pdf", f), purpose="user_data")
        job_file_id = job_file_obj.id
        os.unlink(job_pdf_path)
        print(f"[JOB] Uploaded job description PDF with file_id: {job_file_id}")

        prompt_text = (
    "Læs begge dokumenter og returnér følgende som gyldig JSON:\n\n"
    "{\n"
    "  \"name\": str,\n"
    "  \"email\": str,\n"
    "  \"phone\": str,\n"
    "  \"location\": str,\n"
    "  \"experienceYears\": int,\n"
    "  \"profileSummary\": str,\n"
    "  \"matchScore\": int (0-100),\n"
    "  \"skills\": [str],\n"
    "  \"strengths\": [str],\n"
    "  \"weaknesses\": [str],\n"
    "  \"analysisSummary\": str\n"
    "}\n\n"
    "Tilføj feltet \"experienceYears\" som det estimerede antal år kandidaten har relevant professionel erfaring. "
    "MatchScore skal vurderes ud fra hvor godt kandidatens færdigheder, erfaring og profil matcher kravene i jobbeskrivelsen. "
    "Scoren skal være lav, hvis der er få eller ingen relevante kvalifikationer, selv hvis kandidaten har stærke generelle kompetencer. "
    "Styrker og analyse kan gerne nævne andre kvaliteter, som kunne være værdifulde i teamet, selv hvis de ikke passer 1:1 til jobbet. "
    "Negative ting kunne f.eks. være afstand fra bopæl til jobbet, medmindre det er remote arbejde. "
    "Skills skal kun indeholde ting der rent faktisk nævnes i CV'et. Gæt ikke. "
    "Ingen forklaringer. Kun gyldig JSON. Alt output skal være på dansk."
        )

        response = client.responses.create(
            model="gpt-4-turbo",
            input=[
                {
                    "role": "user",
                    "content": [
                        {"type": "input_file", "file_id": cv_file_id},
                        {"type": "input_file", "file_id": job_file_id},
                        {"type": "input_text", "text": prompt_text},
                    ]
                }
            ]
        )

        output_blocks = response.output
        if not output_blocks:
            raise Exception("No output blocks returned from OpenAI.")

        content = output_blocks[0].content[0].text.strip()


        if content.startswith("```json"):
            content = re.sub(r"^```json\s*", "", content)
            content = re.sub(r"\s*```$", "", content)

        parsed_json = json.loads(content)
        return parsed_json 

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"500: {str(e)}")
