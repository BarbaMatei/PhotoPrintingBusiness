import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-photo-mosaic',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './photo-mosaic.html',
  styleUrl: './photo-mosaic.scss',
})
export class PhotoMosaic {}
