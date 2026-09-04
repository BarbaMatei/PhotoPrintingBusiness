import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { SavedAddressDto } from '../../../../../../core/models/account.model';

@Component({
  selector: 'app-address-list-item',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './address-list-item.html',
  styleUrl: './address-list-item.scss',
})
export class AddressListItem {
  readonly address = input.required<SavedAddressDto>();
  readonly deleting = input(false);

  readonly edit = output<SavedAddressDto>();
  readonly remove = output<string>();
}
