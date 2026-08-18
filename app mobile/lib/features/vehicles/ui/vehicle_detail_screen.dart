import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/util/fcfa.dart';
import '../../auth/providers/auth_providers.dart';
import '../../favorites/providers/favorites_providers.dart';
import '../../negotiations/providers/negotiation_providers.dart';
import '../../negotiations/ui/offer_sheet.dart';
import '../models/vehicle_detail.dart';
import '../models/vehicle_enums.dart';
import '../providers/vehicle_providers.dart';

/// «Détail du véhicule». Galería, precio con indicador, características, équipements,
/// vendeur y contacto. Navegable sin sesión; el corazón y el contacto la exigen.
class VehicleDetailScreen extends ConsumerWidget {
  const VehicleDetailScreen({super.key, required this.slug});
  final String slug;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(vehicleDetailProvider(slug));

    return Scaffold(
      body: async.when(
        loading: () => const _LoadingScaffold(),
        error: (_, _) => _ErrorScaffold(
          onRetry: () => ref.invalidate(vehicleDetailProvider(slug)),
        ),
        data: (vehicle) => _DetailBody(vehicle: vehicle),
      ),
    );
  }
}

class _DetailBody extends ConsumerStatefulWidget {
  const _DetailBody({required this.vehicle});
  final VehicleDetail vehicle;

  @override
  ConsumerState<_DetailBody> createState() => _DetailBodyState();
}

class _DetailBodyState extends ConsumerState<_DetailBody> {
  final _pageCtrl = PageController();
  int _page = 0;

  @override
  void dispose() {
    _pageCtrl.dispose();
    super.dispose();
  }

  Future<void> _toggleFavorite() async {
    final auth = ref.read(authControllerProvider);
    if (auth is! Authenticated) {
      _snack('Connectez-vous pour enregistrer ce véhicule.', login: true);
      return;
    }
    await ref
        .read(favoritesControllerProvider.notifier)
        .toggleById(widget.vehicle.id);
  }

  /// «Contacter» — abre la conversación con un primer mensaje.
  Future<void> _contact() async {
    if (!_requireLogin('contacter le vendeur')) return;
    final message = await _composeMessage();
    if (message == null) return;
    await _startNegotiation(message: message);
  }

  /// «Faire une offre» — importe + mensaje opcional.
  Future<void> _makeOffer() async {
    if (!_requireLogin('faire une offre')) return;
    final input =
        await showOfferSheet(context, listedPrice: widget.vehicle.price);
    if (input == null) return;
    await _startNegotiation(amount: input.amount, message: input.message);
  }

  bool _requireLogin(String action) {
    if (ref.read(authControllerProvider) is Authenticated) return true;
    _snack('Connectez-vous pour $action.', login: true);
    return false;
  }

  Future<String?> _composeMessage() {
    final ctrl = TextEditingController(
        text: 'Bonjour, votre ${widget.vehicle.title} est-il toujours disponible ?');
    return showDialog<String>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Contacter le vendeur'),
        content: TextField(
          controller: ctrl,
          maxLines: 4,
          autofocus: true,
          decoration: const InputDecoration(hintText: 'Votre message…'),
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Annuler')),
          FilledButton(
              onPressed: () => Navigator.pop(context, ctrl.text.trim()),
              child: const Text('Envoyer')),
        ],
      ),
    ).then((v) => (v == null || v.isEmpty) ? null : v);
  }

  Future<void> _startNegotiation({num? amount, String? message}) async {
    try {
      final res = await ref.read(negotiationRepositoryProvider).makeOffer(
            vehicleId: widget.vehicle.id,
            amount: amount,
            message: message,
          );
      if (!mounted) return;
      context.push('/negociations/${res.negotiationId}');
    } catch (_) {
      _snack('Action impossible pour le moment. Réessayez.');
    }
  }

  void _snack(String message, {bool login = false}) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(message),
      action: login
          ? SnackBarAction(
              label: 'Se connecter', onPressed: () => context.push('/login'))
          : null,
    ));
  }

  @override
  Widget build(BuildContext context) {
    final v = widget.vehicle;
    final images = v.imageUrls;
    final favIds = ref.watch(favoritesControllerProvider).ids;
    final isFav = favIds.contains(v.id);
    final indicator = priceIndicatorStyle(v.priceIndicator);

    return CustomScrollView(
      slivers: [
        SliverAppBar(
          pinned: true,
          expandedHeight: 280,
          leading: const _CircleButton(icon: Icons.arrow_back),
          actions: [
            _CircleButton(
              icon: isFav ? Icons.favorite : Icons.favorite_border,
              color: isFav ? AppColors.error : null,
              onTap: _toggleFavorite,
            ),
            const SizedBox(width: 4),
          ],
          flexibleSpace: FlexibleSpaceBar(
            background: _Gallery(
              images: images,
              controller: _pageCtrl,
              current: _page,
              onPageChanged: (i) => setState(() => _page = i),
            ),
          ),
        ),
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(20, 18, 20, 32),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (v.status == 'Reserve' || v.status == 'Vendu')
                  _StatusBanner(status: v.status),
                Text(v.title,
                    style: const TextStyle(
                        fontSize: 22,
                        fontWeight: FontWeight.w800,
                        color: AppColors.navy)),
                const SizedBox(height: 4),
                Text('Réf. ${v.publicReference}',
                    style: const TextStyle(color: AppColors.steel, fontSize: 12)),
                const SizedBox(height: 14),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    Text(fcfa(v.price),
                        style: const TextStyle(
                            fontSize: 26,
                            fontWeight: FontWeight.w900,
                            color: AppColors.azureDark)),
                    const SizedBox(width: 10),
                    if (v.priceNegotiable)
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: AppColors.frostDark,
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: const Text('Négociable',
                            style: TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w600,
                                color: AppColors.steel)),
                      ),
                  ],
                ),
                if (indicator != null) ...[
                  const SizedBox(height: 10),
                  Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 10, vertical: 6),
                    decoration: BoxDecoration(
                      color: indicator.background,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(indicator.icon, size: 16, color: indicator.color),
                        const SizedBox(width: 6),
                        Text(indicator.label,
                            style: TextStyle(
                                fontWeight: FontWeight.w700,
                                color: indicator.color)),
                        if (v.priceComparablesCount > 0)
                          Text('  ·  ${v.priceComparablesCount} annonces comparées',
                              style: TextStyle(
                                  fontSize: 12, color: indicator.color)),
                      ],
                    ),
                  ),
                ],
                const SizedBox(height: 22),
                _SectionTitle('Caractéristiques'),
                _SpecsGrid(vehicle: v),
                if (v.equipments.isNotEmpty) ...[
                  const SizedBox(height: 22),
                  _SectionTitle('Équipements'),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      for (final e in v.equipments)
                        Chip(
                          label: Text(e.name),
                          visualDensity: VisualDensity.compact,
                        ),
                    ],
                  ),
                ],
                if (v.description != null && v.description!.trim().isNotEmpty) ...[
                  const SizedBox(height: 22),
                  _SectionTitle('Description du vendeur'),
                  Text(v.description!,
                      style: const TextStyle(height: 1.5, color: AppColors.navyDark)),
                ],
                const SizedBox(height: 22),
                _SectionTitle('Localisation'),
                Row(
                  children: [
                    const Icon(Icons.place_outlined,
                        size: 18, color: AppColors.steel),
                    const SizedBox(width: 6),
                    Text(
                      [v.district, v.city, v.region]
                          .where((e) => e != null && e.isNotEmpty)
                          .join(', '),
                      style: const TextStyle(color: AppColors.navyDark),
                    ),
                  ],
                ),
                const SizedBox(height: 22),
                _SectionTitle('Vendeur'),
                _SellerCard(
                  vehicle: v,
                  isOwner: switch (ref.watch(authControllerProvider)) {
                    Authenticated(:final user) => user.id == v.sellerId,
                    _ => false,
                  },
                  onContact: _contact,
                  onOffer: _makeOffer,
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _Gallery extends StatelessWidget {
  const _Gallery({
    required this.images,
    required this.controller,
    required this.current,
    required this.onPageChanged,
  });
  final List<String> images;
  final PageController controller;
  final int current;
  final ValueChanged<int> onPageChanged;

  @override
  Widget build(BuildContext context) {
    if (images.isEmpty) {
      return Container(
        color: AppColors.frostDark,
        child: const Center(
          child: Icon(Icons.directions_car_outlined,
              size: 64, color: AppColors.silver),
        ),
      );
    }
    return Stack(
      fit: StackFit.expand,
      children: [
        PageView.builder(
          controller: controller,
          itemCount: images.length,
          onPageChanged: onPageChanged,
          itemBuilder: (_, i) => Image.network(
            images[i],
            fit: BoxFit.cover,
            errorBuilder: (_, _, _) => Container(
              color: AppColors.frostDark,
              child: const Icon(Icons.broken_image_outlined,
                  color: AppColors.silver),
            ),
          ),
        ),
        if (images.length > 1)
          Positioned(
            bottom: 12,
            left: 0,
            right: 0,
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                for (var i = 0; i < images.length; i++)
                  Container(
                    margin: const EdgeInsets.symmetric(horizontal: 3),
                    width: i == current ? 20 : 7,
                    height: 7,
                    decoration: BoxDecoration(
                      color: i == current
                          ? AppColors.white
                          : AppColors.white.withValues(alpha: 0.5),
                      borderRadius: BorderRadius.circular(4),
                    ),
                  ),
              ],
            ),
          ),
      ],
    );
  }
}

class _CircleButton extends StatelessWidget {
  const _CircleButton({required this.icon, this.onTap, this.color});
  final IconData icon;
  final VoidCallback? onTap;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(6),
      child: Material(
        color: Colors.white.withValues(alpha: 0.9),
        shape: const CircleBorder(),
        child: InkWell(
          customBorder: const CircleBorder(),
          onTap: onTap ?? () => Navigator.of(context).maybePop(),
          child: Padding(
            padding: const EdgeInsets.all(8),
            child: Icon(icon, size: 20, color: color ?? AppColors.navy),
          ),
        ),
      ),
    );
  }
}

class _StatusBanner extends StatelessWidget {
  const _StatusBanner({required this.status});
  final String status;

  @override
  Widget build(BuildContext context) {
    final sold = status == 'Vendu';
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: sold ? AppColors.error.withValues(alpha: 0.1) : AppColors.frostDark,
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        children: [
          Icon(sold ? Icons.sell : Icons.lock_clock,
              size: 18, color: sold ? AppColors.error : AppColors.steel),
          const SizedBox(width: 8),
          Text(sold ? 'Véhicule vendu' : 'Véhicule réservé',
              style: TextStyle(
                  fontWeight: FontWeight.w700,
                  color: sold ? AppColors.error : AppColors.steel)),
        ],
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);
  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Text(text,
          style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w800,
              color: AppColors.navy)),
    );
  }
}

class _SpecsGrid extends StatelessWidget {
  const _SpecsGrid({required this.vehicle});
  final VehicleDetail vehicle;

  @override
  Widget build(BuildContext context) {
    final specs = <(IconData, String, String)>[
      (Icons.calendar_today_outlined, 'Année', '${vehicle.year}'),
      if (vehicle.mileage != null)
        (Icons.speed, 'Kilométrage', '${fcfa(vehicle.mileage, withSuffix: false)} km'),
      (Icons.build_circle_outlined, 'État', conditionLabel(vehicle.condition)),
      if (vehicle.fuelType != null)
        (Icons.local_gas_station_outlined, 'Carburant', fuelLabel(vehicle.fuelType)),
      if (vehicle.transmission != null)
        (Icons.settings_outlined, 'Boîte', transmissionLabel(vehicle.transmission)),
      if (vehicle.bodyType != null)
        (Icons.directions_car_outlined, 'Carrosserie', bodyLabel(vehicle.bodyType)),
      if (vehicle.powerCv != null)
        (Icons.flash_on_outlined, 'Puissance', '${vehicle.powerCv} ch'),
      if (vehicle.engineDisplacementCc != null)
        (Icons.tune, 'Cylindrée', '${vehicle.engineDisplacementCc} cm³'),
      if (vehicle.drivetrain != null)
        (Icons.alt_route, 'Transmission', drivetrainLabel(vehicle.drivetrain)),
      if (vehicle.doors != null)
        (Icons.sensor_door_outlined, 'Portes', '${vehicle.doors}'),
      if (vehicle.seats != null)
        (Icons.event_seat_outlined, 'Places', '${vehicle.seats}'),
      if (vehicle.color != null && vehicle.color!.isNotEmpty)
        (Icons.palette_outlined, 'Couleur', vehicle.color!),
    ];

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      padding: EdgeInsets.zero,
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        mainAxisSpacing: 10,
        crossAxisSpacing: 10,
        mainAxisExtent: 62,
      ),
      itemCount: specs.length,
      itemBuilder: (_, i) {
        final (icon, label, value) = specs[i];
        return Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          decoration: BoxDecoration(
            color: AppColors.frost,
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: AppColors.frostDark),
          ),
          child: Row(
            children: [
              Icon(icon, size: 20, color: AppColors.azureDark),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(label,
                        style: const TextStyle(
                            fontSize: 11, color: AppColors.steel)),
                    Text(value,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                            color: AppColors.navy)),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _SellerCard extends StatelessWidget {
  const _SellerCard({
    required this.vehicle,
    required this.isOwner,
    required this.onContact,
    required this.onOffer,
  });
  final VehicleDetail vehicle;
  final bool isOwner;
  final VoidCallback onContact;
  final VoidCallback onOffer;

  @override
  Widget build(BuildContext context) {
    final memberSince = vehicle.sellerMemberSince;
    final accountLabel =
        vehicle.sellerAccountType == 'Professionnel' ? 'Professionnel' : 'Particulier';

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.frost,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.frostDark),
      ),
      child: Column(
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 24,
                backgroundColor: AppColors.navy,
                child: Text(
                  vehicle.sellerName.isNotEmpty
                      ? vehicle.sellerName[0].toUpperCase()
                      : '?',
                  style: const TextStyle(
                      color: AppColors.white, fontWeight: FontWeight.w700),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Flexible(
                          child: Text(vehicle.sellerName,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                  fontSize: 15,
                                  color: AppColors.navy)),
                        ),
                        if (vehicle.sellerPhoneVerified) ...[
                          const SizedBox(width: 6),
                          const Icon(Icons.verified,
                              size: 16, color: AppColors.azureDark),
                        ],
                      ],
                    ),
                    Text(accountLabel,
                        style: const TextStyle(
                            fontSize: 12, color: AppColors.steel)),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              Expanded(
                child: _SellerStat(
                  value: '${vehicle.sellerVerifiedSalesCount}',
                  label: 'Ventes vérifiées',
                ),
              ),
              Container(width: 1, height: 32, color: AppColors.frostDark),
              Expanded(
                child: _SellerStat(
                  value: 'Depuis ${memberSince.year}',
                  label: 'Membre',
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          if (isOwner)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(vertical: 12),
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: AppColors.frostDark,
                borderRadius: BorderRadius.circular(10),
              ),
              child: const Text('C’est votre annonce',
                  style: TextStyle(
                      color: AppColors.steel, fontWeight: FontWeight.w600)),
            )
          else if (vehicle.status == 'Vendu')
            const Text('Ce véhicule est vendu.',
                style: TextStyle(color: AppColors.steel))
          else
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: onContact,
                    icon: const Icon(Icons.chat_bubble_outline, size: 18),
                    label: const Text('Contacter'),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: FilledButton.icon(
                    onPressed: onOffer,
                    icon: const Icon(Icons.local_offer_outlined, size: 18),
                    label: const Text('Offre'),
                  ),
                ),
              ],
            ),
        ],
      ),
    );
  }
}

class _SellerStat extends StatelessWidget {
  const _SellerStat({required this.value, required this.label});
  final String value;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(value,
            style: const TextStyle(
                fontWeight: FontWeight.w800,
                fontSize: 16,
                color: AppColors.navy)),
        Text(label,
            style: const TextStyle(fontSize: 11, color: AppColors.steel)),
      ],
    );
  }
}

class _LoadingScaffold extends StatelessWidget {
  const _LoadingScaffold();

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        const Center(child: CircularProgressIndicator()),
        SafeArea(
          child: Align(
            alignment: Alignment.topLeft,
            child: BackButton(onPressed: () => Navigator.of(context).maybePop()),
          ),
        ),
      ],
    );
  }
}

class _ErrorScaffold extends StatelessWidget {
  const _ErrorScaffold({required this.onRetry});
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Column(
        children: [
          Align(
            alignment: Alignment.topLeft,
            child: BackButton(onPressed: () => Navigator.of(context).maybePop()),
          ),
          const Spacer(),
          const Icon(Icons.error_outline, size: 56, color: AppColors.silver),
          const SizedBox(height: 12),
          const Text('Annonce introuvable',
              style: TextStyle(
                  fontWeight: FontWeight.w700,
                  fontSize: 16,
                  color: AppColors.navy)),
          const SizedBox(height: 6),
          const Text('Elle a peut-être été retirée.',
              style: TextStyle(color: AppColors.steel)),
          const SizedBox(height: 20),
          FilledButton(onPressed: onRetry, child: const Text('Réessayer')),
          const Spacer(flex: 2),
        ],
      ),
    );
  }
}
