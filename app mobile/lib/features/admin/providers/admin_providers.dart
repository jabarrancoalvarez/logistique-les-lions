import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/providers/auth_providers.dart';
import '../data/admin_repository.dart';
import '../models/admin_common.dart';
import '../models/admin_contract.dart';
import '../models/admin_dashboard.dart';
import '../models/admin_listing.dart';
import '../models/admin_negotiation.dart';
import '../models/admin_report.dart';
import '../models/admin_user.dart';

final adminRepositoryProvider = Provider<AdminRepository>(
  (ref) => AdminRepository(ref.watch(apiClientProvider)),
);

final adminDashboardProvider = FutureProvider.autoDispose<AdminDashboard>(
  (ref) => ref.watch(adminRepositoryProvider).getDashboard(),
);

typedef UsersQuery = ({String? search, String? status});

final adminUsersProvider =
    FutureProvider.autoDispose.family<AdminPage<AdminUserRow>, UsersQuery>(
  (ref, q) => ref
      .watch(adminRepositoryProvider)
      .getUsers(search: q.search, status: q.status),
);

final adminUserProvider =
    FutureProvider.autoDispose.family<AdminUserDetail, String>(
  (ref, id) => ref.watch(adminRepositoryProvider).getUser(id),
);

final adminListingsProvider =
    FutureProvider.autoDispose.family<AdminPage<AdminListingRow>, String?>(
  (ref, search) =>
      ref.watch(adminRepositoryProvider).getListings(search: search),
);

final adminListingProvider =
    FutureProvider.autoDispose.family<AdminListingDetail, String>(
  (ref, id) => ref.watch(adminRepositoryProvider).getListing(id),
);

final adminReportsProvider =
    FutureProvider.autoDispose.family<ReportList, String?>(
  (ref, status) =>
      ref.watch(adminRepositoryProvider).getReports(status: status),
);

final adminReportProvider =
    FutureProvider.autoDispose.family<ReportDetail, String>(
  (ref, id) => ref.watch(adminRepositoryProvider).getReport(id),
);

final adminNegotiationsProvider =
    FutureProvider.autoDispose<AdminPage<AdminNegotiationRow>>(
  (ref) => ref.watch(adminRepositoryProvider).getNegotiations(),
);

final adminNegotiationProvider =
    FutureProvider.autoDispose.family<AdminNegotiationDetail, String>(
  (ref, id) => ref.watch(adminRepositoryProvider).getNegotiation(id),
);

final adminContractsProvider =
    FutureProvider.autoDispose<AdminPage<AdminContractRow>>(
  (ref) => ref.watch(adminRepositoryProvider).getContracts(),
);

final adminContractProvider =
    FutureProvider.autoDispose.family<AdminContractDetail, String>(
  (ref, id) => ref.watch(adminRepositoryProvider).getContract(id),
);
