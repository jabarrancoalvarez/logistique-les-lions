/// Las 14 regiones administrativas de Senegal (mismos códigos que la web).
class SenegalRegion {
  final String code;
  final String name;
  const SenegalRegion(this.code, this.name);
}

const senegalRegions = <SenegalRegion>[
  SenegalRegion('DK', 'Dakar'),
  SenegalRegion('DB', 'Diourbel'),
  SenegalRegion('FK', 'Fatick'),
  SenegalRegion('KA', 'Kaffrine'),
  SenegalRegion('KL', 'Kaolack'),
  SenegalRegion('KE', 'Kédougou'),
  SenegalRegion('KD', 'Kolda'),
  SenegalRegion('LG', 'Louga'),
  SenegalRegion('MT', 'Matam'),
  SenegalRegion('SL', 'Saint-Louis'),
  SenegalRegion('SE', 'Sédhiou'),
  SenegalRegion('TC', 'Tambacounda'),
  SenegalRegion('TH', 'Thiès'),
  SenegalRegion('ZG', 'Ziguinchor'),
];
