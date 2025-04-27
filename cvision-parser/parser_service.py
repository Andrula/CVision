from openai import OpenAI
import os
from dotenv import load_dotenv
import fitz  

load_dotenv()
api_key = os.getenv("OPENAI_API_KEY")
client = OpenAI(api_key=api_key)

def extract_text_from_pdf(path):
    doc = fitz.open(path)
    return "\n".join([page.get_text() for page in doc])

def call_openai_parser(cv_text, job_title, job_description):
    prompt = f"""
Du er en HR medarbejder, ansat til og evaluere ansøgere til en stilling.

Følgende er en jobtitel og jobbeskrivelse:

Job titel:
{job_title}

Job beskrivelse:
{job_description}

Her er ét CV fra en medarbejder.
{cv_text}

Dit job er at:
- Analysere ansøgers CV og udtrække struktureret data.
- Derefter skal du analysere ansøger op imod jobstillingen.
- Til sidst skal du returnere et JSON objekt med følgende felter:

Note: 
Til skills skal du kun udtrække relevante skills til stillingen.

{{
  "applicant": {{
    "name": "string",
    "contact": {{
      "email": "string",
      "phone": "string",
      "location": "string"
    }},
    "profile": "string",
    "workExperience": [
      {{
        "position": "string",
        "company": "string",
        "location": "string",
        "timePeriod": "string",
        "responsibilities": ["string", "string"]
      }}
    ],
    "education": [
      {{
        "degree": "string",
        "institution": "string",
        "timePeriod": "string"
      }}
    ],
    "skills": ["string"],
    "languages": ["string"],
    "strengths": ["bullet point analysis based on the job"],
    "weaknesses": ["bullet point analysis based on the job"],
    "analysisSummary": "a paragraph summarizing how this candidate fits or doesn't",
    "matchScore": "number from 0 to 100 based on how well this CV matches the job"
  }}
}}

Output kun JSON.
"""  
    response = client.chat.completions.create(
        model="gpt-4",
        messages=[
            {"role": "user", "content": prompt}
        ]
    )

    return response.choices[0].message.content
