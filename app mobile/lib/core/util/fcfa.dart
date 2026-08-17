/// Formatea un importe en francos CFA con el formato del documento funcional:
/// `8900000` → `8.900.000 FCFA`. Yoon u Auto trabaja solo en FCFA (XOF).
String fcfa(num? value, {bool withSuffix = true}) {
  if (value == null) return '';
  final rounded = value.round();
  final digits = rounded.abs().toString();
  final buf = StringBuffer();
  for (var i = 0; i < digits.length; i++) {
    if (i > 0 && (digits.length - i) % 3 == 0) buf.write('.');
    buf.write(digits[i]);
  }
  final sign = rounded < 0 ? '-' : '';
  final formatted = '$sign$buf';
  return withSuffix ? '$formatted FCFA' : formatted;
}
