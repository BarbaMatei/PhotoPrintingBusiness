import { Component } from '@angular/core';

@Component({
  selector: 'app-terms',
  template: `
    <div class="legal-page">
      <h1>Termeni și condiții</h1>
      <p>Conținut în curs de redactare.</p>
    </div>
  `,
  styles: [`.legal-page { padding: 2rem; max-width: 720px; margin: 0 auto; }`],
})
export class TermsPage {}
