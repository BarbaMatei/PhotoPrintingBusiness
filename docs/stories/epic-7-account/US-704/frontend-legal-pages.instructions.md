# US-704 — Legal Pages & Cookie Consent (Frontend)

## Story
**As a** visitor  
**I want to** read legal documents and manage cookie preferences

## Type
FRONTEND — Angular

## Epic
EPIC-7 | Cont Utilizator & Legal

## Dependencies
- US-804 (Angular App Shell — footer links)

## Acceptance Criteria

1. **Static pages**: `/politica-de-confidentialitate`, `/termeni-si-conditii`, `/politica-cookie`
2. **Cookie consent banner** on first visit: `Acceptă toate` / `Doar esențiale`; stored in localStorage
3. **MVP**: only functional cookies (session, auth); no analytics/tracking by default
4. **Footer** on all pages links to all three legal pages and to company info (CUI, Nr. Reg. Com.)

## Technical Notes

### Component Location
`src/app/features/legal/`

### Implementation Details
- Three static page components with hardcoded Romanian legal text
- Content can be stored as Markdown and rendered, or as plain HTML templates
- Cookie consent banner:
  - Check localStorage for `cookieConsent` key on app init
  - If not set: show bottom banner with two buttons
  - `Acceptă toate`: set `cookieConsent = 'all'`
  - `Doar esențiale`: set `cookieConsent = 'essential'`
  - Banner hidden after choice; preference persisted
  - MVP: no difference in behavior (no analytics cookies used)
- Footer component: links to all 3 legal pages + company registration info
- Legal content should be reviewed by a legal professional before production deployment

### Legal Pages Content (Romanian)
- Politica de confidențialitate: GDPR compliance text, data collection, rights
- Termeni și condiții: service terms, liability, returns policy
- Politica cookie: cookie types used, consent management

## Files to Create/Modify
- `src/app/features/legal/privacy/privacy.component.ts`
- `src/app/features/legal/terms/terms.component.ts`
- `src/app/features/legal/cookies/cookies.component.ts`
- `src/app/shared/components/cookie-consent/cookie-consent.component.ts`
- `src/app/shared/components/footer/footer.component.ts` (add legal links)

## Testing
- Unit test: cookie consent banner shows on first visit
- Unit test: banner hidden after consent
- Unit test: consent stored in localStorage
- Unit test: legal pages render content
- E2E: cookie consent flow
