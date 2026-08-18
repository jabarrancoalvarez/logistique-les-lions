/// Criterios de búsqueda del Marketplace. Un subconjunto de los ~30 filtros de la
/// web: los más usados en móvil. Se serializan a la query string de `/vehicles`.
class VehicleFilters {
  final String? search;
  final String? makeId;
  final num? priceFrom;
  final num? priceTo;
  final int? yearFrom;
  final int? yearTo;
  final int? mileageTo;
  final String? region;
  final String? fuelType;
  final String? transmission;
  final String? bodyType;
  final String? condition; // New | Used | Km0
  final String sortBy;
  final bool sortDesc;

  const VehicleFilters({
    this.search,
    this.makeId,
    this.priceFrom,
    this.priceTo,
    this.yearFrom,
    this.yearTo,
    this.mileageTo,
    this.region,
    this.fuelType,
    this.transmission,
    this.bodyType,
    this.condition,
    this.sortBy = 'createdAt',
    this.sortDesc = true,
  });

  /// Número de filtros activos (para el badge del botón «Filtres»), sin contar
  /// la búsqueda de texto ni la ordenación.
  int get activeCount {
    var n = 0;
    if (makeId != null) n++;
    if (priceFrom != null || priceTo != null) n++;
    if (yearFrom != null || yearTo != null) n++;
    if (mileageTo != null) n++;
    if (region != null) n++;
    if (fuelType != null) n++;
    if (transmission != null) n++;
    if (bodyType != null) n++;
    if (condition != null) n++;
    return n;
  }

  VehicleFilters copyWith({
    Object? search = _keep,
    Object? makeId = _keep,
    Object? priceFrom = _keep,
    Object? priceTo = _keep,
    Object? yearFrom = _keep,
    Object? yearTo = _keep,
    Object? mileageTo = _keep,
    Object? region = _keep,
    Object? fuelType = _keep,
    Object? transmission = _keep,
    Object? bodyType = _keep,
    Object? condition = _keep,
    String? sortBy,
    bool? sortDesc,
  }) {
    return VehicleFilters(
      search: search == _keep ? this.search : search as String?,
      makeId: makeId == _keep ? this.makeId : makeId as String?,
      priceFrom: priceFrom == _keep ? this.priceFrom : priceFrom as num?,
      priceTo: priceTo == _keep ? this.priceTo : priceTo as num?,
      yearFrom: yearFrom == _keep ? this.yearFrom : yearFrom as int?,
      yearTo: yearTo == _keep ? this.yearTo : yearTo as int?,
      mileageTo: mileageTo == _keep ? this.mileageTo : mileageTo as int?,
      region: region == _keep ? this.region : region as String?,
      fuelType: fuelType == _keep ? this.fuelType : fuelType as String?,
      transmission:
          transmission == _keep ? this.transmission : transmission as String?,
      bodyType: bodyType == _keep ? this.bodyType : bodyType as String?,
      condition: condition == _keep ? this.condition : condition as String?,
      sortBy: sortBy ?? this.sortBy,
      sortDesc: sortDesc ?? this.sortDesc,
    );
  }

  /// Query string para `/vehicles`. La paginación la añade el repositorio.
  Map<String, dynamic> toQueryParameters() {
    final q = <String, dynamic>{
      'sortBy': sortBy,
      'sortDesc': sortDesc,
    };
    void put(String k, Object? v) {
      if (v != null && !(v is String && v.trim().isEmpty)) q[k] = v;
    }

    put('search', search);
    put('makeId', makeId);
    put('priceFrom', priceFrom);
    put('priceTo', priceTo);
    put('yearFrom', yearFrom);
    put('yearTo', yearTo);
    put('mileageTo', mileageTo);
    put('region', region);
    put('fuelType', fuelType);
    put('transmission', transmission);
    put('bodyType', bodyType);
    put('condition', condition);
    return q;
  }

  static const _keep = Object();
}
