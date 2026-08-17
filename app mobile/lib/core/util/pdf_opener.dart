import 'dart:io';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';

/// Guarda unos bytes de PDF en un archivo temporal y lo abre con el visor del
/// sistema. Devuelve `true` si se pudo abrir.
Future<bool> saveAndOpenPdf(List<int> bytes, String fileName) async {
  try {
    final dir = await getTemporaryDirectory();
    final safe = fileName.endsWith('.pdf') ? fileName : '$fileName.pdf';
    final file = File('${dir.path}/$safe');
    await file.writeAsBytes(bytes, flush: true);
    final result = await OpenFilex.open(file.path, type: 'application/pdf');
    return result.type == ResultType.done;
  } catch (_) {
    return false;
  }
}
