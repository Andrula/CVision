import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import translationEN from './locales/en.json';
import translationDA from './locales/da.json';

const resources = {
  en: {
    translation: translationEN
  },
  da: {
    translation: translationDA
  }
};

// Get saved language from localStorage or default to Danish
const savedLanguage = localStorage.getItem('language') || 'da';

i18n
  .use(initReactI18next)
  .init({
    resources,
    lng: savedLanguage,
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false
    }
  });

export default i18n;
