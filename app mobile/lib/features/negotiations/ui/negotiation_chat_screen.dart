import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../../../core/util/time_ago.dart';
import '../../auth/providers/auth_providers.dart';
import '../models/chat_message.dart';
import '../models/negotiation_detail.dart';
import '../models/negotiation_enums.dart';
import '../providers/negotiation_providers.dart';
import 'offer_sheet.dart';

/// Chat de una negociación: cronología, mensajes en vivo (SignalR), ofertas y
/// contacto. El contrato completo llega en una entrega posterior.
class NegotiationChatScreen extends ConsumerStatefulWidget {
  const NegotiationChatScreen({super.key, required this.negotiationId});
  final String negotiationId;

  @override
  ConsumerState<NegotiationChatScreen> createState() =>
      _NegotiationChatScreenState();
}

class _NegotiationChatScreenState
    extends ConsumerState<NegotiationChatScreen> {
  final _input = TextEditingController();
  final _scroll = ScrollController();
  int _lastCount = 0;

  @override
  void dispose() {
    _input.dispose();
    _scroll.dispose();
    super.dispose();
  }

  void _scrollToEnd() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scroll.hasClients) {
        _scroll.animateTo(
          _scroll.position.maxScrollExtent,
          duration: const Duration(milliseconds: 250),
          curve: Curves.easeOut,
        );
      }
    });
  }

  Future<void> _send() async {
    final text = _input.text.trim();
    if (text.isEmpty) return;
    _input.clear();
    final ok =
        await ref.read(chatControllerProvider(widget.negotiationId).notifier).send(text);
    if (!ok && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Message non envoyé. Réessayez.')),
      );
      _input.text = text;
    }
  }

  Future<void> _counterOffer(NegotiationDetail detail) async {
    final input = await showOfferSheet(context,
        listedPrice: detail.vehiclePrice, title: 'Contre-offre');
    if (input == null) return;
    final err = await ref
        .read(chatControllerProvider(widget.negotiationId).notifier)
        .makeCounterOffer(input.amount, input.message);
    _feedback(err);
  }

  Future<void> _respondOffer(String offerId, bool accept) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: Text(accept ? 'Accepter l’offre ?' : 'Refuser l’offre ?'),
        content: Text(accept
            ? 'En acceptant, vous convenez du prix avec l’autre partie.'
            : 'L’offre sera refusée. Vous pourrez toujours contre-proposer.'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Annuler')),
          FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: Text(accept ? 'Accepter' : 'Refuser')),
        ],
      ),
    );
    if (confirmed != true) return;
    final notifier =
        ref.read(chatControllerProvider(widget.negotiationId).notifier);
    final err =
        accept ? await notifier.acceptOffer(offerId) : await notifier.rejectOffer(offerId);
    _feedback(err);
  }

  void _feedback(String? err) {
    if (!mounted || err == null) return;
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(err)));
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(chatControllerProvider(widget.negotiationId));
    final myId = switch (ref.watch(authControllerProvider)) {
      Authenticated(:final user) => user.id,
      _ => null,
    };

    // Auto-scroll cuando llegan mensajes nuevos.
    if (state.messages.length != _lastCount) {
      _lastCount = state.messages.length;
      _scrollToEnd();
    }

    final detail = state.detail;

    return Scaffold(
      appBar: AppBar(
        title: Text(detail?.otherUserName ?? 'Négociation',
            style: const TextStyle(fontSize: 16)),
        actions: detail == null
            ? null
            : [
                IconButton(
                  tooltip: 'Inspection',
                  icon: const Icon(Icons.fact_check_outlined),
                  onPressed: () => context
                      .push('/negociations/${widget.negotiationId}/inspection'),
                ),
                IconButton(
                  tooltip: 'Contrat',
                  icon: const Icon(Icons.description_outlined),
                  onPressed: () async {
                    await context
                        .push('/negociations/${widget.negotiationId}/contrat');
                    // El contrato puede haber cambiado el estado (venta validée).
                    await ref
                        .read(chatControllerProvider(widget.negotiationId)
                            .notifier)
                        .refreshDetail();
                  },
                ),
              ],
        bottom: state.otherTyping
            ? const PreferredSize(
                preferredSize: Size.fromHeight(18),
                child: Padding(
                  padding: EdgeInsets.only(bottom: 4),
                  child: Text('en train d’écrire…',
                      style: TextStyle(
                          fontSize: 12, color: AppColors.azureLight)),
                ),
              )
            : null,
      ),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error || detail == null
              ? _ErrorBody(
                  onRetry: () => ref
                      .read(chatControllerProvider(widget.negotiationId).notifier)
                      .load())
              : Column(
                  children: [
                    _VehicleHeader(detail: detail),
                    Expanded(
                        child: _Conversation(
                            state: state, myId: myId, scroll: _scroll)),
                    if (detail.pendingOffer != null)
                      _PendingOfferBar(
                        detail: detail,
                        busy: state.actionBusy,
                        onAccept: () =>
                            _respondOffer(detail.pendingOffer!.id, true),
                        onReject: () =>
                            _respondOffer(detail.pendingOffer!.id, false),
                        onCounter: () => _counterOffer(detail),
                      ),
                    _InputBar(
                      controller: _input,
                      sending: state.sending,
                      enabled: detail.acceptsNegotiation,
                      onSend: _send,
                      onOffer: detail.acceptsNegotiation
                          ? () => _counterOffer(detail)
                          : null,
                      onTyping: () => ref
                          .read(chatControllerProvider(widget.negotiationId)
                              .notifier)
                          .notifyTyping(),
                    ),
                  ],
                ),
    );
  }
}

class _VehicleHeader extends StatelessWidget {
  const _VehicleHeader({required this.detail});
  final NegotiationDetail detail;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppColors.frost,
      child: InkWell(
        onTap: () => context.push('/vehicules/${detail.vehicleSlug}'),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          child: Row(
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(6),
                child: SizedBox(
                  width: 44,
                  height: 44,
                  child: detail.vehicleThumbnailUrl != null
                      ? Image.network(detail.vehicleThumbnailUrl!,
                          fit: BoxFit.cover,
                          errorBuilder: (_, _, _) => const ColoredBox(
                              color: AppColors.frostDark,
                              child: Icon(Icons.directions_car_outlined,
                                  size: 20, color: AppColors.silver)))
                      : const ColoredBox(
                          color: AppColors.frostDark,
                          child: Icon(Icons.directions_car_outlined,
                              size: 20, color: AppColors.silver)),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(detail.vehicleTitle,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                            fontWeight: FontWeight.w700,
                            fontSize: 13,
                            color: AppColors.navy)),
                    Text(fcfa(detail.vehiclePrice),
                        style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: AppColors.azureDark)),
                  ],
                ),
              ),
              if (!detail.acceptsNegotiation)
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: AppColors.frostDark,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(detail.vehicleStatus == 'Vendu' ? 'Vendu' : 'Fermé',
                      style: const TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w600,
                          color: AppColors.steel)),
                )
              else
                const Icon(Icons.chevron_right, color: AppColors.steel),
            ],
          ),
        ),
      ),
    );
  }
}

/// Cronología + mensajes fusionados por fecha.
class _Conversation extends StatelessWidget {
  const _Conversation(
      {required this.state, required this.myId, required this.scroll});
  final ChatState state;
  final String? myId;
  final ScrollController scroll;

  @override
  Widget build(BuildContext context) {
    final items = _merge(state);
    if (items.isEmpty) {
      return const Center(
        child: Text('Démarrez la conversation.',
            style: TextStyle(color: AppColors.steel)),
      );
    }
    return ListView.builder(
      controller: scroll,
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
      itemCount: items.length,
      itemBuilder: (_, i) {
        final item = items[i];
        if (item is _EventEntry) {
          return _EventChip(event: item);
        }
        final msg = (item as _MessageEntry).message;
        final mine = msg.senderId == myId;
        return _Bubble(message: msg, mine: mine);
      },
    );
  }

  List<_Entry> _merge(ChatState state) {
    final entries = <_Entry>[
      for (final m in state.messages) _MessageEntry(m),
      // Los hitos «conversación iniciada» y de mensajes no aportan como chip.
      for (final e in state.detail?.timeline ?? const [])
        if (e.type != 'ConversationStarted')
          _EventEntry(e.type, e.amount, e.byMe, e.createdAt),
    ];
    entries.sort((a, b) => a.when.compareTo(b.when));
    return entries;
  }
}

sealed class _Entry {
  DateTime get when;
}

class _MessageEntry extends _Entry {
  _MessageEntry(this.message);
  final ChatMessage message;
  @override
  DateTime get when => message.createdAt;
}

class _EventEntry extends _Entry {
  _EventEntry(this.type, this.amount, this.byMe, this._when);
  final String type;
  final num? amount;
  final bool byMe;
  final DateTime _when;
  @override
  DateTime get when => _when;
}

class _EventChip extends StatelessWidget {
  const _EventChip({required this.event});
  final _EventEntry event;

  @override
  Widget build(BuildContext context) {
    final text = eventLabel(event.type,
        byMe: event.byMe,
        amountLabel: event.amount != null ? fcfa(event.amount) : null);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Center(
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
          decoration: BoxDecoration(
            color: AppColors.frostDark,
            borderRadius: BorderRadius.circular(20),
          ),
          child: Text(text,
              textAlign: TextAlign.center,
              style: const TextStyle(
                  fontSize: 12,
                  color: AppColors.steel,
                  fontWeight: FontWeight.w600)),
        ),
      ),
    );
  }
}

class _Bubble extends StatelessWidget {
  const _Bubble({required this.message, required this.mine});
  final ChatMessage message;
  final bool mine;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: mine ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        constraints: BoxConstraints(
            maxWidth: MediaQuery.of(context).size.width * 0.75),
        margin: const EdgeInsets.symmetric(vertical: 4),
        padding: const EdgeInsets.fromLTRB(12, 8, 12, 6),
        decoration: BoxDecoration(
          color: mine ? AppColors.azureDark : AppColors.white,
          borderRadius: BorderRadius.only(
            topLeft: const Radius.circular(14),
            topRight: const Radius.circular(14),
            bottomLeft: Radius.circular(mine ? 14 : 4),
            bottomRight: Radius.circular(mine ? 4 : 14),
          ),
          border: mine ? null : Border.all(color: AppColors.frostDark),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(message.body,
                style: TextStyle(
                    color: mine ? AppColors.white : AppColors.navyDark,
                    height: 1.35)),
            const SizedBox(height: 2),
            Text(messageTime(message.createdAt),
                style: TextStyle(
                    fontSize: 10,
                    color: mine
                        ? AppColors.white.withValues(alpha: 0.7)
                        : AppColors.steel)),
          ],
        ),
      ),
    );
  }
}

class _PendingOfferBar extends StatelessWidget {
  const _PendingOfferBar({
    required this.detail,
    required this.busy,
    required this.onAccept,
    required this.onReject,
    required this.onCounter,
  });
  final NegotiationDetail detail;
  final bool busy;
  final VoidCallback onAccept;
  final VoidCallback onReject;
  final VoidCallback onCounter;

  @override
  Widget build(BuildContext context) {
    final offer = detail.pendingOffer!;
    return Container(
      width: double.infinity,
      color: AppColors.frost,
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.local_offer_outlined,
                  size: 18, color: AppColors.azureDark),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  offer.byMe
                      ? 'Votre offre : ${fcfa(offer.amount)}'
                      : 'Offre reçue : ${fcfa(offer.amount)}',
                  style: const TextStyle(
                      fontWeight: FontWeight.w700, color: AppColors.navy),
                ),
              ),
              Text(offerStatusLabel(offer.status),
                  style: const TextStyle(
                      fontSize: 12, color: AppColors.steel)),
            ],
          ),
          if (offer.message != null && offer.message!.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 4, left: 26),
              child: Text('« ${offer.message!} »',
                  style: const TextStyle(
                      fontSize: 12,
                      fontStyle: FontStyle.italic,
                      color: AppColors.steel)),
            ),
          const SizedBox(height: 8),
          if (busy)
            const Center(
                child: Padding(
              padding: EdgeInsets.all(6),
              child: SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2)),
            ))
          else if (offer.canRespond)
            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: onReject,
                    style: OutlinedButton.styleFrom(
                        foregroundColor: AppColors.error,
                        side: const BorderSide(color: AppColors.error)),
                    child: const Text('Refuser'),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: OutlinedButton(
                    onPressed: onCounter,
                    child: const Text('Contre-offre'),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: FilledButton(
                    onPressed: onAccept,
                    child: const Text('Accepter'),
                  ),
                ),
              ],
            )
          else
            const Text('En attente de la réponse de l’autre partie.',
                style: TextStyle(fontSize: 12, color: AppColors.steel)),
        ],
      ),
    );
  }
}

class _InputBar extends StatelessWidget {
  const _InputBar({
    required this.controller,
    required this.sending,
    required this.enabled,
    required this.onSend,
    required this.onTyping,
    this.onOffer,
  });
  final TextEditingController controller;
  final bool sending;
  final bool enabled;
  final VoidCallback onSend;
  final VoidCallback onTyping;
  final VoidCallback? onOffer;

  @override
  Widget build(BuildContext context) {
    if (!enabled) {
      return Container(
        width: double.infinity,
        color: AppColors.frostDark,
        padding: const EdgeInsets.all(14),
        child: const SafeArea(
          top: false,
          child: Text(
            'Cette annonce n’accepte plus de nouveaux messages.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.steel),
          ),
        ),
      );
    }
    return SafeArea(
      top: false,
      child: Container(
        decoration: const BoxDecoration(
          color: AppColors.white,
          border: Border(top: BorderSide(color: AppColors.frostDark)),
        ),
        padding: const EdgeInsets.fromLTRB(8, 8, 8, 8),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            if (onOffer != null)
              IconButton(
                onPressed: onOffer,
                icon: const Icon(Icons.local_offer_outlined),
                color: AppColors.azureDark,
                tooltip: 'Faire une offre',
              ),
            Expanded(
              child: TextField(
                controller: controller,
                minLines: 1,
                maxLines: 4,
                textInputAction: TextInputAction.newline,
                onChanged: (_) => onTyping(),
                decoration: const InputDecoration(
                  hintText: 'Écrire un message…',
                  isDense: true,
                  border: OutlineInputBorder(),
                ),
              ),
            ),
            const SizedBox(width: 6),
            sending
                ? const Padding(
                    padding: EdgeInsets.all(10),
                    child: SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(strokeWidth: 2)),
                  )
                : IconButton.filled(
                    onPressed: onSend,
                    icon: const Icon(Icons.send),
                  ),
          ],
        ),
      ),
    );
  }
}

class _ErrorBody extends StatelessWidget {
  const _ErrorBody({required this.onRetry});
  final VoidCallback onRetry;
  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off, size: 48, color: AppColors.silver),
          const SizedBox(height: 12),
          const Text('Impossible d’ouvrir la négociation',
              style: TextStyle(color: AppColors.steel)),
          const SizedBox(height: 16),
          FilledButton(onPressed: onRetry, child: const Text('Réessayer')),
        ],
      ),
    );
  }
}
