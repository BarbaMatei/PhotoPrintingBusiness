import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Header } from './layout/header/header';
import { Footer } from './layout/footer/footer';
import { ToastComponent } from './shared/components/toast/toast';
import { CookieConsentBanner } from './shared/components/cookie-consent/cookie-consent';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, Footer, ToastComponent, CookieConsentBanner],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
