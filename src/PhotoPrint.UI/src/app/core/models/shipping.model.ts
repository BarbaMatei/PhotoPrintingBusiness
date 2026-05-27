export interface LockerDto {
  id: string;
  samedayId: string;
  name: string;
  address: string;
  city: string;
  lat: number;
  lng: number;
}

export interface ShippingCostDto {
  costRon: number;
}

export type DeliveryType = 'Easybox' | 'Courier';

export interface ShippingAddressForm {
  street: string;
  number: string;
  block: string;
  city: string;
  county: string;
  postalCode: string;
  recipientName: string;
  phone: string;
}

export interface DeliveryState {
  method: DeliveryType | null;
  lockerId: string | null;
  lockerName: string | null;
  shippingAddress: ShippingAddressForm | null;
  shippingCostRon: number;
}

export const ROMANIAN_COUNTIES = [
  'Alba', 'Arad', 'Argeș', 'Bacău', 'Bihor', 'Bistrița-Năsăud', 'Botoșani', 'Brăila',
  'Brașov', 'București', 'Buzău', 'Călărași', 'Caraș-Severin', 'Cluj', 'Constanța',
  'Covasna', 'Dâmbovița', 'Dolj', 'Galați', 'Giurgiu', 'Gorj', 'Harghita', 'Hunedoara',
  'Ialomița', 'Iași', 'Ilfov', 'Maramureș', 'Mehedinți', 'Mureș', 'Neamț', 'Olt',
  'Prahova', 'Sălaj', 'Satu Mare', 'Sibiu', 'Suceava', 'Teleorman', 'Timiș', 'Tulcea',
  'Vâlcea', 'Vaslui', 'Vrancea',
];
