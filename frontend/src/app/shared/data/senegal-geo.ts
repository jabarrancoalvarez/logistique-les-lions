/**
 * Catálogo geográfico de Senegal: 14 regiones administrativas y sus principales
 * ciudades. Fuente única de verdad para los filtros de ubicación, el alta de
 * anuncios y el perfil del usuario.
 *
 * NOTA: en la parte P34 (Administration → Configuration) este catálogo pasará a
 * mantenerse en base de datos para poder ampliarlo sin tocar código.
 */

export interface SenegalRegion {
  /** Código estable, usado como valor en filtros y persistencia */
  readonly code: string;
  /** Nombre en francés — es el único que se muestra al usuario */
  readonly name: string;
  readonly cities: readonly string[];
}

export const SENEGAL_REGIONS: readonly SenegalRegion[] = [
  {
    code: 'DK',
    name: 'Dakar',
    cities: ['Dakar', 'Guédiawaye', 'Pikine', 'Rufisque', 'Keur Massar', 'Bargny', 'Diamniadio']
  },
  {
    code: 'DB',
    name: 'Diourbel',
    cities: ['Diourbel', 'Touba', 'Mbacké', 'Bambey']
  },
  {
    code: 'FK',
    name: 'Fatick',
    cities: ['Fatick', 'Foundiougne', 'Gossas', 'Sokone', 'Passy']
  },
  {
    code: 'KA',
    name: 'Kaffrine',
    cities: ['Kaffrine', 'Birkelane', 'Koungheul', 'Malem Hodar']
  },
  {
    code: 'KL',
    name: 'Kaolack',
    cities: ['Kaolack', 'Guinguinéo', 'Nioro du Rip', 'Ndoffane']
  },
  {
    code: 'KE',
    name: 'Kédougou',
    cities: ['Kédougou', 'Salémata', 'Saraya']
  },
  {
    code: 'KD',
    name: 'Kolda',
    cities: ['Kolda', 'Vélingara', 'Médina Yoro Foulah']
  },
  {
    code: 'LG',
    name: 'Louga',
    cities: ['Louga', 'Kébémer', 'Linguère', 'Dahra']
  },
  {
    code: 'MT',
    name: 'Matam',
    cities: ['Matam', 'Ourossogui', 'Kanel', 'Thilogne', 'Ranérou']
  },
  {
    code: 'SL',
    name: 'Saint-Louis',
    cities: ['Saint-Louis', 'Richard-Toll', 'Dagana', 'Podor', 'Ross Béthio']
  },
  {
    code: 'SE',
    name: 'Sédhiou',
    cities: ['Sédhiou', 'Bounkiling', 'Goudomp', 'Marsassoum']
  },
  {
    code: 'TC',
    name: 'Tambacounda',
    cities: ['Tambacounda', 'Bakel', 'Goudiry', 'Koumpentoum']
  },
  {
    code: 'TH',
    name: 'Thiès',
    cities: ['Thiès', 'Mbour', 'Saly', 'Tivaouane', 'Joal-Fadiouth', 'Pout', 'Khombole', 'Kayar']
  },
  {
    code: 'ZG',
    name: 'Ziguinchor',
    cities: ['Ziguinchor', 'Bignona', 'Oussouye', 'Cap Skirring']
  }
];

/** Todas las ciudades del país, ordenadas alfabéticamente. */
export const SENEGAL_CITIES: readonly string[] = SENEGAL_REGIONS
  .flatMap(r => r.cities)
  .sort((a, b) => a.localeCompare(b, 'fr'));

/** Ciudades de una región concreta. Devuelve `[]` si el código no existe. */
export function citiesOfRegion(regionCode: string | null | undefined): readonly string[] {
  if (!regionCode) return [];
  return SENEGAL_REGIONS.find(r => r.code === regionCode)?.cities ?? [];
}

/** Región a la que pertenece una ciudad, o `null` si no está en el catálogo. */
export function regionOfCity(city: string | null | undefined): SenegalRegion | null {
  if (!city) return null;
  return SENEGAL_REGIONS.find(r => r.cities.includes(city)) ?? null;
}
