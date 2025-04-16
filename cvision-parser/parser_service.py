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
You are an expert HR recruiter helping to evaluate applicants for a job.

The following is a job title and description:

Job Title:
{job_title}

Job Description:
{job_description}

Here is the full CV of an applicant:
{cv_text}

Your task:
- Parse the applicant’s CV and extract structured data.
- Assess the CV’s relevance to the job description.
- Return a JSON object with the following fields:

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

Output only the JSON.
"""  
    response = client.chat.completions.create(
        model="gpt-4",
        messages=[
            {"role": "user", "content": prompt}
        ]
    )

    return response.choices[0].message.content
