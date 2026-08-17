import 'dart:io';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';

/// Guarda unos bytes de PDF en un archivo temporal y lo abre con el visor del
/// sistema. Devuelve `true` si se pudo abrir.
Future<bool> saveAndOpenPdf(List<int> bytes, String fileName) =>
    saveAndOpenFile(bytes, fileName, 'application/pdf');

/// Guarda unos bytes en un archivo temporal y lo abre con la app del sistema.
Future<bool> saveAndOpenFile(
    List<int> bytes, String fileName, String? contentType) async {
  try {
    final dir = await getTemporaryDirectory();
    final safe = fileName.isEmpty ? 'fichier' : fileName;
    final file = File('${dir.path}/$safe');
    await file.writeAsBytes(bytes, flush: true);
    final result = await OpenFilex.open(file.path, type: contentType);
    return result.type == ResultType.done;
  } catch (_) {
    return false;
  }
}
