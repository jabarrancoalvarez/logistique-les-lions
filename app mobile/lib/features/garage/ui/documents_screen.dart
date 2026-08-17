import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/pdf_opener.dart';
import '../models/garage_document.dart';
import '../providers/garage_providers.dart';

/// Documents — historial documental privado de un vehículo del garaje.
class DocumentsScreen extends ConsumerStatefulWidget {
  const DocumentsScreen({super.key, required this.vehicleId});
  final String vehicleId;

  @override
  ConsumerState<DocumentsScreen> createState() => _DocumentsScreenState();
}

class _DocumentsScreenState extends ConsumerState<DocumentsScreen> {
  bool _busy = false;

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(documentsProvider(widget.vehicleId));

    return Scaffold(
      appBar: AppBar(title: const Text('Documents')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _busy ? null : _upload,
        icon: const Icon(Icons.upload_file),
        label: const Text('Ajouter'),
      ),
      body: Stack(
        children: [
          async.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (_, _) => Center(
              child: FilledButton(
                onPressed: () =>
                    ref.invalidate(documentsProvider(widget.vehicleId)),
                child: const Text('Réessayer'),
              ),
            ),
            data: (docs) {
              if (docs.isEmpty) return const _Empty();
              return ListView.separated(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
                itemCount: docs.length,
                separatorBuilder: (_, _) => const SizedBox(height: 10),
                itemBuilder: (_, i) => _DocTile(
                  doc: docs[i],
                  onOpen: () => _open(docs[i]),
                  onDelete: () => _delete(docs[i]),
                ),
              );
            },
          ),
          if (_busy)
            const Positioned.fill(
              child: ColoredBox(
                color: Color(0x66000000),
                child: Center(child: CircularProgressIndicator()),
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _upload() async {
    final picked = await FilePickerPlatform.instance.pickFiles(
      type: FileType.custom,
      allowedExtensions: ['pdf', 'jpg', 'jpeg', 'png', 'webp'],
    );
    final file = picked.firstOrNull;
    if (file == null) return;

    final type = await _pickType();
    if (type == null) return;

    setState(() => _busy = true);
    try {
      final bytes = await file.readAsBytes();
      await ref.read(garageRepositoryProvider).uploadDocument(
            widget.vehicleId,
            bytes: bytes,
            filename: file.name,
            contentType: _contentType(file.name),
            type: type,
          );
      ref.invalidate(documentsProvider(widget.vehicleId));
    } catch (_) {
      _snack('Envoi impossible.');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<String?> _pickType() {
    return showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      builder: (_) => SafeArea(
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Padding(
                padding: EdgeInsets.all(16),
                child: Text('Type de document',
                    style: TextStyle(
                        fontWeight: FontWeight.w700, fontSize: 16)),
              ),
              for (final t in garageDocumentTypeValues)
                ListTile(
                  title: Text(garageDocumentTypeLabel(t)),
                  onTap: () => Navigator.pop(context, t),
                ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _open(GarageDocument doc) async {
    setState(() => _busy = true);
    try {
      final bytes =
          await ref.read(garageRepositoryProvider).documentBytes(doc.id);
      final ok = await saveAndOpenFile(bytes, doc.fileName, doc.contentType);
      if (!ok) _snack('Aucune application pour ouvrir ce fichier.');
    } catch (_) {
      _snack('Téléchargement impossible.');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _delete(GarageDocument doc) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Supprimer le document ?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Annuler')),
          FilledButton(
              style: FilledButton.styleFrom(backgroundColor: AppColors.error),
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Supprimer')),
        ],
      ),
    );
    if (ok != true) return;
    try {
      await ref.read(garageRepositoryProvider).deleteDocument(doc.id);
      ref.invalidate(documentsProvider(widget.vehicleId));
    } catch (_) {
      _snack('Suppression impossible.');
    }
  }

  void _snack(String m) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(m)));
  }

  static String _contentType(String name) {
    final n = name.toLowerCase();
    if (n.endsWith('.pdf')) return 'application/pdf';
    if (n.endsWith('.png')) return 'image/png';
    if (n.endsWith('.webp')) return 'image/webp';
    return 'image/jpeg';
  }
}

class _DocTile extends StatelessWidget {
  const _DocTile(
      {required this.doc, required this.onOpen, required this.onDelete});
  final GarageDocument doc;
  final VoidCallback onOpen;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: EdgeInsets.zero,
      child: ListTile(
        onTap: onOpen,
        leading: Icon(doc.isPdf ? Icons.picture_as_pdf_outlined : Icons.image_outlined,
            color: AppColors.azureDark),
        title: Text(doc.name.isEmpty ? doc.fileName : doc.name,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontWeight: FontWeight.w700)),
        subtitle: Text([
          garageDocumentTypeLabel(doc.type),
          if (doc.documentDate != null)
            DateFormat('d MMM yyyy', 'fr').format(doc.documentDate!.toLocal()),
          _size(doc.sizeBytes),
        ].join(' · ')),
        trailing: IconButton(
          icon: const Icon(Icons.delete_outline, color: AppColors.steel),
          onPressed: onDelete,
        ),
      ),
    );
  }

  String _size(int bytes) {
    if (bytes < 1024) return '$bytes o';
    if (bytes < 1024 * 1024) return '${(bytes / 1024).round()} Ko';
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} Mo';
  }
}

class _Empty extends StatelessWidget {
  const _Empty();
  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Padding(
        padding: EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.folder_outlined, size: 56, color: AppColors.silver),
            SizedBox(height: 16),
            Text('Aucun document',
                style: TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 16,
                    color: AppColors.navy)),
            SizedBox(height: 6),
            Text('Ajoutez la carte grise, les factures ou l’assurance.',
                textAlign: TextAlign.center,
                style: TextStyle(color: AppColors.steel)),
          ],
        ),
      ),
    );
  }
}
