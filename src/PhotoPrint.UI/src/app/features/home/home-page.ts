import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductService } from '../../core/services/product.service';
import { PricingTier } from '../../core/models/product.model';

@Component({
  selector: 'app-home-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DecimalPipe],
  template: `
    <!-- â•â• HERO â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• -->
    <section class="hero">
      <div class="hero__content">
        <div class="hero__eyebrow">
          <span class="hero__eyebrow-dot"></span>
          Tipărire foto premium în România
        </div>
        <h1 class="hero__headline">
          Amintirile tale,<br>
          <em class="hero__headline-em">tipărite cu grijă</em>
        </h1>
        <p class="hero__sub">
          Fotografii de calitate superioară pe hârtie Fujifilm, livrate rapid în toată țara.
          Format clasic, finisaj mat sau lucios — alegi tu.
        </p>
        <div class="hero__actions">
          <a routerLink="/tipareste" class="btn btn--accent btn--xl">
            Tipărește acum
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
          </a>
          <a routerLink="/preturi" class="btn btn--ghost btn--lg">
            Vezi prețurile
          </a>
        </div>
        <div class="hero__trust">
          <div class="hero__trust-item">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
            Hârtie foto Fujifilm
          </div>
          <div class="hero__trust-item">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
            Livrare 1–3 zile
          </div>
          <div class="hero__trust-item">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
            Prețuri de la 0.89 lei/buc
          </div>
        </div>
      </div>

      <div class="hero__visual" aria-hidden="true">
        <div class="photo-mosaic">
          <div class="photo-mosaic__frame photo-mosaic__frame--a">
            <div class="photo-mosaic__inner">
              <div class="photo-mosaic__img photo-mosaic__img--warm">🌅</div>
            </div>
          </div>
          <div class="photo-mosaic__frame photo-mosaic__frame--b">
            <div class="photo-mosaic__inner">
              <div class="photo-mosaic__img photo-mosaic__img--cool">📷</div>
            </div>
          </div>
          <div class="photo-mosaic__frame photo-mosaic__frame--c">
            <div class="photo-mosaic__inner">
              <div class="photo-mosaic__img photo-mosaic__img--rose">❤️</div>
            </div>
          </div>
          <div class="photo-mosaic__frame photo-mosaic__frame--d">
            <div class="photo-mosaic__inner">
              <div class="photo-mosaic__img photo-mosaic__img--green">🌿</div>
            </div>
          </div>
          <div class="photo-mosaic__badge">
            <span class="photo-mosaic__badge-num">10×</span>
            <span class="photo-mosaic__badge-label">mai ieftin în volum</span>
          </div>
        </div>
      </div>
    </section>

    <!-- â•â• INFINITE SCROLL STRIP â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• -->
    <div class="format-strip" aria-hidden="true">
      <div class="format-strip__track">
        @for (label of formatLabels; track label; let i = $index) {
          <div class="format-strip__item">{{ label }}</div>
          @if (i < formatLabels.length - 1) {
            <span class="format-strip__sep">·</span>
          }
        }
        @for (label of formatLabels; track label + '_dup') {
          <div class="format-strip__item">{{ label }}</div>
          <span class="format-strip__sep">·</span>
        }
      </div>
    </div>

    <!-- â•â• HOW IT WORKS â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• -->
    <section class="steps section section--soft">
      <div class="container">
        <div class="steps__header">
          <h2 class="steps__title">Cum funcționează?</h2>
          <p class="steps__sub">Trei paşi simpli de la fotografie la amintire tipărită.</p>
        </div>
        <div class="steps__grid">
          <div class="step">
            <div class="step__num">01</div>
            <div class="step__icon-wrap">
              <svg class="step__icon" viewBox="0 0 48 48" fill="none">
                <circle cx="24" cy="24" r="22" fill="#e8f0fe"/>
                <path d="M14 28V18a2 2 0 012-2h16a2 2 0 012 2v10" stroke="#1a73e8" stroke-width="2" stroke-linecap="round"/>
                <path d="M10 28h28" stroke="#1a73e8" stroke-width="2" stroke-linecap="round"/>
                <path d="M20 18l4-4 4 4" stroke="#1a73e8" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <h3 class="step__name">Încarci fotografiile</h3>
            <p class="step__desc">Selectezi pozele de pe dispozitiv. Acceptăm JPEG, PNG – până la 100 fotografii odată.</p>
          </div>
          <div class="step__connector" aria-hidden="true"></div>
          <div class="step">
            <div class="step__num">02</div>
            <div class="step__icon-wrap">
              <svg class="step__icon" viewBox="0 0 48 48" fill="none">
                <circle cx="24" cy="24" r="22" fill="#fff3e0"/>
                <rect x="13" y="15" width="22" height="18" rx="2" stroke="#ff6d00" stroke-width="2"/>
                <path d="M13 21h22M19 21v12" stroke="#ff6d00" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </div>
            <h3 class="step__name">Alegi formatul</h3>
            <p class="step__desc">10×15, 13×18, A4 şi altele. Mat sau lucios. Prețul se actualizează instant.</p>
          </div>
          <div class="step__connector" aria-hidden="true"></div>
          <div class="step">
            <div class="step__num">03</div>
            <div class="step__icon-wrap">
              <svg class="step__icon" viewBox="0 0 48 48" fill="none">
                <circle cx="24" cy="24" r="22" fill="#e6f4ea"/>
                <path d="M12 28l5-5 5 5 9-9" stroke="#1e8e3e" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M33 24v8H15v-8" stroke="#1e8e3e" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </div>
            <h3 class="step__name">Primeşti acasă</h3>
            <p class="step__desc">Livrăm în 1–3 zile lucrătoare prin curier rapid sau la un punct EasyBox.</p>
          </div>
        </div>
      </div>
    </section>

    <!-- â•â• QUALITY HIGHLIGHT â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• -->
    <section class="quality section">
      <div class="container">
        <div class="quality__layout">
          <div class="quality__visual" aria-hidden="true">
            <div class="quality__frame">
              <div class="quality__photo">📸</div>
              <div class="quality__label">Fujifilm Crystal Archive</div>
            </div>
            <div class="quality__stat">
              <strong>98%</strong>
              <span>clienți mulțumiți</span>
            </div>
          </div>
          <div class="quality__content">
            <span class="quality__tag badge badge--blue badge--lg">De ce FotoTipar?</span>
            <h2 class="quality__title">
              Calitatea<br>care se vede şi se simte
            </h2>
            <p class="quality__body">
              Folosim exclusiv hârtie fotografică <strong>Fujifilm Crystal Archive</strong> —
              standard de industrie pentru culori fidele, contrast profund şi durabilitate de
              zeci de ani. Fiecare comandă este procesată cu atenție la detalii.
            </p>
            <ul class="quality__list">
              <li>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#1e8e3e" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
                Culori fidele fotografiei originale
              </li>
              <li>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#1e8e3e" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
                Finisaj mat sau lucios, la alegere
              </li>
              <li>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#1e8e3e" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
                Margini precise, fără tăieri neașteptate
              </li>
              <li>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#1e8e3e" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
                Ambalare sigură — ajung fără îndoituri
              </li>
            </ul>
            <a routerLink="/tipareste" class="btn btn--primary btn--lg">
              Comandă acum
            </a>
          </div>
        </div>
      </div>
    </section>

    <!-- â•â• PRICING TEASE â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• -->
    <section class="pricing-tease section section--soft">
      <div class="container">
        <h2 class="pricing-tease__title">Prețuri avantajoase cu cât tipăreşti mai mult</h2>
        <p class="pricing-tease__sub">Cu cât e mai mare comanda, cu atât scade prețul per fotografie.</p>
        <div class="pricing-tease__cards">
          @for (card of pricingCards(); track card.tierLabel; let i = $index) {
            <div class="pricing-tease__card" [class.pricing-tease__card--popular]="i === 1">
              @if (i === 1) { <div class="pricing-tease__badge">Cel mai ales</div> }
              <div class="pricing-tease__range">{{ card.range }}</div>
              <div class="pricing-tease__price">{{ card.unitPrice | number:'1.2-2' }}<span>lei/buc</span></div>
              <div class="pricing-tease__name">{{ card.tierLabel }}</div>
            </div>
          }
          @if (pricingCards().length === 0) {
            <div class="pricing-tease__card">
              <div class="pricing-tease__range">1–9 buc</div>
              <div class="pricing-tease__price">1.20<span>lei/buc</span></div>
              <div class="pricing-tease__name">Standard</div>
            </div>
            <div class="pricing-tease__card pricing-tease__card--popular">
              <div class="pricing-tease__badge">Cel mai ales</div>
              <div class="pricing-tease__range">10–49 buc</div>
              <div class="pricing-tease__price">0.99<span>lei/buc</span></div>
              <div class="pricing-tease__name">Popular</div>
            </div>
            <div class="pricing-tease__card">
              <div class="pricing-tease__range">50+ buc</div>
              <div class="pricing-tease__price">0.89<span>lei/buc</span></div>
              <div class="pricing-tease__name">Volum</div>
            </div>
          }
        </div>
        @if (pricingProductName()) {
          <p class="pricing-tease__source">Prețuri pentru <strong>{{ pricingProductName() }}</strong>.</p>
        }
        <p class="pricing-tease__note">
          Prețurile variază în funcție de format. Calculul se face automat.
          <a routerLink="/preturi">Vezi toate formatele →</a>
        </p>
      </div>
    </section>

    <!-- â•â• CTA BANNER â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• -->
    <section class="cta-section section">
      <div class="container">
        <div class="cta-block">
          <div class="cta-block__content">
            <h2 class="cta-block__title">Gata să tipăreşti amintirile tale?</h2>
            <p class="cta-block__sub">Câteva clicuri şi fotografiile tale ajung la uşa ta în 1–3 zile.</p>
          </div>
          <div class="cta-block__actions">
            <a routerLink="/tipareste" class="btn btn--accent btn--xl">
              Începe acum
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
            </a>
          </div>
        </div>
      </div>
    </section>
  `,
  styles: [`
    @use 'styles/variables' as *;
    @use 'styles/mixins' as *;

    /* â•â• HERO â•â• */
    .hero {
      display: grid;
      grid-template-columns: 1fr 1fr;
      align-items: center;
      gap: $space-12;
      max-width: 1160px;
      margin: 0 auto;
      padding: clamp(3rem, 8vh, 6rem) $space-6;

      @media (max-width: #{$breakpoint-md}) {
        grid-template-columns: 1fr;
        text-align: center;
        padding: $space-10 $space-4 $space-8;
      }
    }

    .hero__content {
      display: flex;
      flex-direction: column;
      gap: $space-5;
      @media (max-width: #{$breakpoint-md}) { align-items: center; }
    }

    .hero__eyebrow {
      display: inline-flex;
      align-items: center;
      gap: $space-2;
      font-size: $font-size-xs;
      font-weight: $font-weight-semi;
      letter-spacing: 0.1em;
      text-transform: uppercase;
      color: $color-primary;
    }

    .hero__eyebrow-dot {
      width: 6px;
      height: 6px;
      border-radius: $radius-full;
      background: $color-accent;
      animation: pulse-dot 2s ease-in-out infinite;
    }

    @keyframes pulse-dot {
      0%, 100% { transform: scale(1); opacity: 1; }
      50%       { transform: scale(1.5); opacity: 0.6; }
    }

    .hero__headline {
      font-family: $font-family-display;
      font-size: clamp(2.4rem, 5vw, 3.8rem);
      font-weight: 700;
      line-height: 1.1;
      color: $color-neutral-900;
      margin: 0;
      letter-spacing: -0.02em;
    }

    .hero__headline-em {
      font-style: italic;
      background: linear-gradient(135deg, $color-primary 0%, $color-accent 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    .hero__sub {
      font-size: $font-size-lg;
      color: $color-neutral-500;
      line-height: $line-height-relaxed;
      max-width: 500px;
      margin: 0;
      @media (max-width: #{$breakpoint-md}) { max-width: 420px; }
    }

    .hero__actions {
      display: flex;
      align-items: center;
      gap: $space-3;
      flex-wrap: wrap;
      @media (max-width: #{$breakpoint-md}) { justify-content: center; }
    }

    .hero__trust {
      display: flex;
      gap: $space-5;
      flex-wrap: wrap;
      @media (max-width: #{$breakpoint-md}) { justify-content: center; }
    }

    .hero__trust-item {
      display: inline-flex;
      align-items: center;
      gap: $space-1 + 0.125rem;
      font-size: $font-size-sm;
      color: $color-neutral-500;
      font-weight: $font-weight-medium;

      svg { color: $color-success; flex-shrink: 0; }
    }

    /* â•â• PHOTO MOSAIC â•â• */
    .hero__visual {
      display: flex;
      justify-content: center;
      @media (max-width: #{$breakpoint-md}) { display: none; }
    }

    .photo-mosaic {
      position: relative;
      width: 380px;
      height: 380px;
    }

    .photo-mosaic__frame {
      position: absolute;
      background: $color-white;
      border: 2px solid $color-neutral-300;
      border-radius: $radius-xl;
      box-shadow: $shadow-lg;
      overflow: hidden;
      transition: transform $transition-slow;

      &:hover { transform: scale(1.03) rotate(0deg) !important; }

      &--a {
        width: 200px; height: 170px;
        top: 0; left: 40px;
        transform: rotate(-3deg);
        animation: float-a 7s ease-in-out infinite;
        z-index: 3;
      }

      &--b {
        width: 170px; height: 150px;
        top: 20px; right: 0;
        transform: rotate(4deg);
        animation: float-b 9s ease-in-out infinite;
        z-index: 2;
      }

      &--c {
        width: 180px; height: 190px;
        bottom: 30px; left: 0;
        transform: rotate(2deg);
        animation: float-c 8s ease-in-out infinite;
        z-index: 2;
      }

      &--d {
        width: 160px; height: 160px;
        bottom: 10px; right: 20px;
        transform: rotate(-2deg);
        animation: float-d 10s ease-in-out infinite;
        z-index: 3;
      }
    }

    .photo-mosaic__inner {
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .photo-mosaic__img {
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 4rem;

      &--warm  { background: linear-gradient(135deg, #fce4b8, #f5cba7); }
      &--cool  { background: linear-gradient(135deg, #b8d4f5, #a0c4f0); }
      &--rose  { background: linear-gradient(135deg, #f5b8c4, #f0a0b4); }
      &--green { background: linear-gradient(135deg, #b8f5c4, #a0f0b0); }
    }

    .photo-mosaic__badge {
      position: absolute;
      bottom: 80px;
      left: 50%;
      transform: translateX(-50%);
      background: $color-white;
      border: 1.5px solid $color-neutral-300;
      border-radius: $radius-full;
      padding: $space-2 $space-4;
      box-shadow: $shadow-md;
      display: flex;
      flex-direction: column;
      align-items: center;
      z-index: 10;
      white-space: nowrap;
    }

    .photo-mosaic__badge-num {
      font-size: $font-size-xl;
      font-weight: 800;
      color: $color-accent;
      line-height: 1;
    }

    .photo-mosaic__badge-label {
      font-size: $font-size-xs;
      color: $color-neutral-500;
      font-weight: $font-weight-medium;
    }

    @keyframes float-a {
      0%, 100% { transform: rotate(-3deg) translateY(0); }
      50%       { transform: rotate(-3deg) translateY(-8px); }
    }
    @keyframes float-b {
      0%, 100% { transform: rotate(4deg) translateY(0); }
      50%       { transform: rotate(4deg) translateY(-6px); }
    }
    @keyframes float-c {
      0%, 100% { transform: rotate(2deg) translateY(0); }
      50%       { transform: rotate(2deg) translateY(-10px); }
    }
    @keyframes float-d {
      0%, 100% { transform: rotate(-2deg) translateY(0); }
      50%       { transform: rotate(-2deg) translateY(-7px); }
    }

    /* â•â• FORMAT STRIP â•â• */
    .format-strip {
      width: 100%;
      overflow: hidden;
      border-top: 1px solid $color-neutral-300;
      border-bottom: 1px solid $color-neutral-300;
      background: $color-bg-soft;
      padding: $space-3 0;
    }

    .format-strip__track {
      display: flex;
      align-items: center;
      gap: $space-4;
      animation: marquee 24s linear infinite;
      width: max-content;
    }

    .format-strip__item {
      font-size: $font-size-sm;
      font-weight: $font-weight-semi;
      color: $color-neutral-500;
      letter-spacing: 0.06em;
      white-space: nowrap;
      text-transform: uppercase;
    }

    .format-strip__sep {
      color: $color-accent;
      font-weight: $font-weight-bold;
    }

    @keyframes marquee {
      from { transform: translateX(0); }
      to   { transform: translateX(-50%); }
    }

    /* â•â• STEPS â•â• */
    .steps__header {
      text-align: center;
      margin-bottom: $space-12;
    }

    .steps__title {
      @include section-title;
    }

    .steps__sub {
      font-size: $font-size-base;
      color: $color-neutral-500;
      margin: $space-2 auto 0;
      max-width: 400px;
      line-height: $line-height-relaxed;
    }

    .steps__grid {
      display: flex;
      align-items: flex-start;
      justify-content: center;
      gap: 0;
      max-width: 900px;
      margin: 0 auto;

      @media (max-width: #{$breakpoint-md}) {
        flex-direction: column;
        align-items: center;
        gap: $space-8;
      }
    }

    .step {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      flex: 1;
      max-width: 260px;
      padding: $space-6;
    }

    .step__connector {
      flex-shrink: 0;
      width: 60px;
      height: 2px;
      background: linear-gradient(90deg, $color-primary-light, $color-accent-light);
      margin-top: 40px;
      border-radius: $radius-full;

      @media (max-width: #{$breakpoint-md}) {
        width: 2px;
        height: 40px;
        margin: 0;
        background: linear-gradient(180deg, $color-primary-light, $color-accent-light);
      }
    }

    .step__num {
      font-family: $font-family-display;
      font-size: $font-size-4xl;
      font-weight: 700;
      color: $color-neutral-300;
      line-height: 1;
      margin-bottom: $space-3;
    }

    .step__icon-wrap {
      margin-bottom: $space-4;
    }

    .step__icon {
      width: 56px;
      height: 56px;
    }

    .step__name {
      font-size: $font-size-lg;
      font-weight: $font-weight-bold;
      color: $color-neutral-900;
      margin: 0 0 $space-2;
    }

    .step__desc {
      font-size: $font-size-sm;
      color: $color-neutral-500;
      line-height: $line-height-relaxed;
      margin: 0;
    }

    /* â•â• QUALITY â•â• */
    .quality__layout {
      display: grid;
      grid-template-columns: 1fr 1.3fr;
      gap: $space-16;
      align-items: center;
      max-width: 1000px;
      margin: 0 auto;

      @media (max-width: #{$breakpoint-md}) {
        grid-template-columns: 1fr;
        gap: $space-10;
      }
    }

    .quality__visual {
      position: relative;
      @media (max-width: #{$breakpoint-md}) { order: 2; }
    }

    .quality__frame {
      background: linear-gradient(145deg, #fce4b8 0%, #f5cba7 60%, #e8d4c0 100%);
      border-radius: 24px;
      aspect-ratio: 4/5;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: $space-3;
      box-shadow: $shadow-xl;
      position: relative;
      overflow: hidden;

      &::before {
        content: '';
        position: absolute;
        inset: 0;
        background: url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='0.08'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E");
        opacity: 0.5;
      }
    }

    .quality__photo {
      font-size: 5rem;
      filter: drop-shadow(0 4px 16px rgba(0,0,0,0.15));
    }

    .quality__label {
      background: rgba(255,255,255,0.85);
      backdrop-filter: blur(8px);
      border-radius: $radius-full;
      padding: $space-1 + 0.125rem $space-4;
      font-size: $font-size-xs;
      font-weight: $font-weight-semi;
      color: $color-neutral-700;
      letter-spacing: 0.03em;
    }

    .quality__stat {
      position: absolute;
      bottom: -$space-4;
      right: -$space-4;
      background: $color-white;
      border-radius: $radius-xl;
      padding: $space-4 $space-5;
      box-shadow: $shadow-xl;
      display: flex;
      flex-direction: column;
      align-items: center;

      strong {
        font-size: $font-size-3xl;
        font-weight: 800;
        color: $color-primary;
        line-height: 1;
      }

      span {
        font-size: $font-size-xs;
        color: $color-neutral-500;
        white-space: nowrap;
        margin-top: $space-1;
      }
    }

    .quality__content {
      display: flex;
      flex-direction: column;
      gap: $space-5;
    }

    .quality__tag { align-self: flex-start; }

    .quality__title {
      font-family: $font-family-display;
      font-size: clamp(1.8rem, 3.5vw, 2.5rem);
      font-weight: 700;
      line-height: $line-height-snug;
      color: $color-neutral-900;
      margin: 0;
      letter-spacing: -0.02em;
    }

    .quality__body {
      font-size: $font-size-base;
      color: $color-neutral-500;
      line-height: $line-height-relaxed;
      margin: 0;
    }

    .quality__list {
      list-style: none;
      padding: 0;
      margin: 0;
      display: flex;
      flex-direction: column;
      gap: $space-3;

      li {
        display: flex;
        align-items: center;
        gap: $space-2 + 0.125rem;
        font-size: $font-size-base;
        color: $color-neutral-700;
        font-weight: $font-weight-medium;

        svg { flex-shrink: 0; }
      }
    }

    /* â•â• PRICING TEASE â•â• */
    .pricing-tease__title {
      @include section-title;
      margin-bottom: $space-2;
    }

    .pricing-tease__sub {
      font-size: $font-size-base;
      color: $color-neutral-500;
      text-align: center;
      margin-bottom: $space-10;
    }

    .pricing-tease__cards {
      display: flex;
      justify-content: center;
      gap: $space-4;
      flex-wrap: wrap;
      max-width: 720px;
      margin: 0 auto;
      padding-top: $space-5;
    }

    .pricing-tease__card {
      @include card;
      overflow: visible;
      flex: 1;
      min-width: 180px;
      max-width: 220px;
      padding: $space-6 $space-5;
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      gap: $space-2;
      position: relative;

      &--popular {
        border-color: $color-accent;
        box-shadow: $shadow-glow-accent;
        transform: scale(1.04);
      }
    }

    .pricing-tease__badge {
      position: absolute;
      top: -$space-3;
      left: 50%;
      transform: translateX(-50%);
      background: linear-gradient(135deg, $color-accent, #ff9100);
      color: $color-white;
      border-radius: $radius-full;
      padding: 0.2rem $space-3;
      font-size: $font-size-xs;
      font-weight: $font-weight-bold;
      white-space: nowrap;
    }

    .pricing-tease__range {
      font-size: $font-size-sm;
      color: $color-neutral-500;
      font-weight: $font-weight-medium;
    }

    .pricing-tease__price {
      font-size: $font-size-3xl;
      font-weight: 800;
      color: $color-neutral-900;
      line-height: 1;

      span {
        font-size: $font-size-sm;
        font-weight: $font-weight-medium;
        color: $color-neutral-500;
        margin-left: 2px;
      }
    }

    .pricing-tease__name {
      font-size: $font-size-xs;
      font-weight: $font-weight-semi;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: $color-neutral-500;
    }

    .pricing-tease__source {
      text-align: center;
      margin-top: $space-6;
      font-size: $font-size-sm;
      color: $color-neutral-500;
    }

    .pricing-tease__note {
      text-align: center;
      margin-top: $space-3;
      font-size: $font-size-sm;
      color: $color-neutral-500;

      a {
        color: $color-primary;
        font-weight: $font-weight-medium;
        &:hover { text-decoration: underline; }
      }
    }

    /* â•â• CTA BLOCK â•â• */
    .cta-block {
      background: linear-gradient(135deg, $color-neutral-900 0%, #2d1d0e 100%);
      border-radius: 24px;
      padding: clamp(2.5rem, 5vw, 4rem) clamp(2rem, 4vw, 5rem);
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: $space-8;
      position: relative;
      overflow: hidden;

      &::before {
        content: '';
        position: absolute;
        top: -60px; right: -60px;
        width: 240px; height: 240px;
        background: radial-gradient(circle, rgba($color-accent, 0.25) 0%, transparent 70%);
      }

      &::after {
        content: '';
        position: absolute;
        bottom: -40px; left: -40px;
        width: 200px; height: 200px;
        background: radial-gradient(circle, rgba($color-primary, 0.20) 0%, transparent 70%);
      }

      @media (max-width: #{$breakpoint-md}) {
        flex-direction: column;
        text-align: center;
        padding: $space-10 $space-6;
      }
    }

    .cta-block__content { position: relative; z-index: 1; }

    .cta-block__title {
      font-family: $font-family-display;
      font-size: clamp(1.6rem, 3vw, 2.2rem);
      font-weight: 700;
      color: $color-white;
      margin: 0 0 $space-2;
      line-height: $line-height-snug;
    }

    .cta-block__sub {
      font-size: $font-size-base;
      color: rgba($color-white, 0.65);
      margin: 0;
    }

    .cta-block__actions {
      position: relative;
      z-index: 1;
      flex-shrink: 0;
    }
  `],
})
export class HomePage implements OnInit {
  private readonly productService = inject(ProductService);

  readonly formatLabels = [
    'Format 10×15', 'Format 13×18', 'Format 15×21', 'Format A4',
    'Finisaj Mat', 'Finisaj Lucios', 'Format 20×30', 'Format 30×40',
    'Format Panoramic', 'Format Pătrat', 'Format A3',
  ];

  private readonly catalogSignal = signal<{ name: string; tiers: PricingTier[] } | null>(null);

  readonly pricingProductName = computed(() => this.catalogSignal()?.name ?? '');

  readonly pricingCards = computed(() => {
    const tiers = this.catalogSignal()?.tiers ?? [];
    const labels = ['Standard', 'Popular', 'Volum'];
    return tiers.slice(0, 3).map((tier, i) => ({
      range: tier.maxQuantity !== null
        ? `${tier.minQuantity}–${tier.maxQuantity} buc`
        : `${tier.minQuantity}+ buc`,
      unitPrice: tier.unitPrice,
      tierLabel: labels[i] ?? `Nivel ${i + 1}`,
    }));
  });

  ngOnInit(): void {
    this.productService.getCatalog().subscribe({
      next: (products) => {
        const first = products.find(p => p.sizes.length > 0);
        if (!first) return;
        const firstSize = first.sizes[0];
        this.catalogSignal.set({ name: `${first.name} – ${firstSize.label}`, tiers: firstSize.pricingTiers });
      },
    });
  }
}

