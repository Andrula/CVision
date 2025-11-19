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

    font_path = os.path.join("fonts", "DejaVuSans.ttf")
    pdf.add_font("DejaVu", "", font_path, uni=True)
    pdf.set_font("DejaVu", size=12)

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

        prompt_text = """
        Læs begge dokumenter og returnér følgende som gyldig JSON:

        {
          "name": str,
          "email": str,
          "phone": str,
          "location": str,
          "experienceYears": int,
          "profileSummary": str,
          "matchScore": int (0-100),
          "skills": [str],  // En liste af enkeltstående faglige færdigheder nævnt i CV’et. Hver skill skal være konkret og stå alene – fx "C#", "truckcertifikat", "kundebetjening", "lagerstyring", "Python", "kassebetjening", "Excel". Undgå at gruppere flere i én entry.
          "strengths": [str],
          "weaknesses": [str],
          "analysisSummary": str
        }

        Tilføj feltet "experienceYears" som det estimerede antal år kandidaten har relevant professionel erfaring.

        Scoring:
        - 90-100: Kandidaten opfylder næsten alle krav i jobopslaget og har stærk relevant erfaring og motivation.
        - 80-89: Kandidaten opfylder mange krav og har god erfaring og potentiale.
        - 70-79: Kandidaten matcher en del af kravene og har relevant erfaring, men mangler nogle centrale kompetencer.
        - 60-69: Kandidaten har delvis erfaring eller viden, men matcher ikke hovedkravene.
        - 0-59: Kandidaten har meget lidt relevant erfaring eller kvalifikationer i forhold til jobopslaget.

        Scoren skal være så objektiv som muligt og tage højde for både dokumenteret erfaring og nævnte færdigheder i forhold til jobopslaget.

        "Styrker og analyse kan gerne nævne andre kvaliteter, som kunne være værdifulde i teamet, selv hvis de ikke passer 1:1 til jobbet. Svagheder kunne være ting som afstand fra bopæl til jobbet, medmindre det er remote arbejde."

        "Skills må ikke samles som én sætning eller liste. Hver entry i listen skal være en enkelt, specifik færdighed nævnt i CV’et uanset om det er en teknisk, praktisk eller kundevendt kompetence. Inkludér kun færdigheder der er relevante for den pågældende stilling (jobbeskrivelsen). Udelad bløde kompetencer eller generelle erfaringer som ikke understøtter det tekniske arbejde, medmindre det nævnes som vigtigt i jobopslaget."

        Ingen forklaringer. Kun gyldig JSON. Alt output skal være på dansk.
        """


        response = client.responses.create(
            model="gpt-4o-mini",
            temperature=0,
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
    
if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
