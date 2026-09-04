import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-format-strip',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './format-strip.html',
  styleUrl: './format-strip.scss',
})
export class FormatStrip {
  readonly formatLabels = [
    'Format 10×15',
    'Format 13×18',
    'Format 15×21',
    'Format A4',
    'Finisaj Mat',
    'Finisaj Lucios',
    'Format 20×30',
    'Format 30×40',
    'Format Panoramic',
    'Format Pătrat',
    'Format A3',
  ];
}
