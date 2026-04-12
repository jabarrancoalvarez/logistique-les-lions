-- ============================================================
-- LOGISTIQUE LES LIONS — Seed Data (datos ficticios para tests)
-- Ejecutar: psql -U lll_user -d logistique_les_lions -f seed_data.sql
-- Contraseña de todos los usuarios de test: Test1234!
-- ============================================================

BEGIN;

-- ─── USUARIOS (M6) ──────────────────────────────────────────────────────────
-- Hash BCrypt de "Test1234!" generado con cost=11
INSERT INTO users.user_profiles
  (id, email, password_hash, first_name, last_name, phone, role,
   country_code, city, company_name, company_vat, bio,
   is_verified, is_active, created_at, updated_at)
VALUES
  ('00000000-0000-0000-0000-000000000001',
   'admin@logistiqueleslions.com',
   '$2a$11$hdbWk8SnCobj5qgIvEP9WuqHuQ7axI7jaPNJA8C90tT5mmd2YVGgu',
   'Sofía','Administradora','+34600000001','Admin',
   'ES','Madrid',NULL,NULL,'Administradora de la plataforma',
   true,true,NOW() - INTERVAL '180 days',NOW()),

  ('00000000-0000-0000-0000-000000000002',
   'dealer@autoiberica.es',
   '$2a$11$hdbWk8SnCobj5qgIvEP9WuqHuQ7axI7jaPNJA8C90tT5mmd2YVGgu',
   'Carlos','Martínez','+34 666 123 456','Dealer',
   'ES','Sevilla','Auto Ibérica S.L.','ES-B91234567',
   'Concesionario especializado en exportación a Marruecos y Norte de África. +15 años de experiencia.',
   true,true,NOW() - INTERVAL '90 days',NOW()),

  ('00000000-0000-0000-0000-000000000003',
   'hans.mueller@gmail.de',
   '$2a$11$hdbWk8SnCobj5qgIvEP9WuqHuQ7axI7jaPNJA8C90tT5mmd2YVGgu',
   'Hans','Müller','+49 176 55678901','Seller',
   'DE','München',NULL,NULL,
   'Particular. Vendo mi flota personal. Todos los vehículos con historial completo.',
   true,true,NOW() - INTERVAL '60 days',NOW()),

  ('00000000-0000-0000-0000-000000000004',
   'youssef.benali@hotmail.com',
   '$2a$11$hdbWk8SnCobj5qgIvEP9WuqHuQ7axI7jaPNJA8C90tT5mmd2YVGgu',
   'Youssef','Ben Ali','+212 661 987654','Buyer',
   'MA','Casablanca',NULL,NULL,
   'Importador de vehículos europeos para el mercado marroquí.',
   true,true,NOW() - INTERVAL '45 days',NOW()),

  ('00000000-0000-0000-0000-000000000005',
   'tanaka.hiroshi@yahoo.co.jp',
   '$2a$11$hdbWk8SnCobj5qgIvEP9WuqHuQ7axI7jaPNJA8C90tT5mmd2YVGgu',
   'Hiroshi','Tanaka','+81 90 1234 5678','Seller',
   'JP','Tokyo',NULL,NULL,
   'Japanese car enthusiast selling high-quality vehicles for export.',
   true,true,NOW() - INTERVAL '40 days',NOW()),

  ('00000000-0000-0000-0000-000000000006',
   'marie.dupont@orange.fr',
   '$2a$11$hdbWk8SnCobj5qgIvEP9WuqHuQ7axI7jaPNJA8C90tT5mmd2YVGgu',
   'Marie','Dupont','+33 6 12 34 56 78','Buyer',
   'FR','Paris',NULL,NULL,NULL,
   false,true,NOW() - INTERVAL '20 days',NOW()),

  ('00000000-0000-0000-0000-000000000007',
   'james.auto@premiumcars.co.uk',
   '$2a$11$hdbWk8SnCobj5qgIvEP9WuqHuQ7axI7jaPNJA8C90tT5mmd2YVGgu',
   'James','Anderson','+44 7700 900123','Dealer',
   'GB','London','Premium Cars UK Ltd','GB-123456789',
   'UK dealer specialising in Continental European luxury vehicles.',
   true,true,NOW() - INTERVAL '30 days',NOW()),

  ('00000000-0000-0000-0000-000000000008',
   'moderador@logistiqueleslions.com',
   '$2a$11$hdbWk8SnCobj5qgIvEP9WuqHuQ7axI7jaPNJA8C90tT5mmd2YVGgu',
   'Pedro','López','+34 691 000 999','Moderator',
   'ES','Barcelona',NULL,NULL,'Moderador de contenido.',
   true,true,NOW() - INTERVAL '120 days',NOW())
ON CONFLICT (id) DO NOTHING;

-- ─── MARCAS (M1) ────────────────────────────────────────────────────────────
INSERT INTO vehicles.vehicle_makes
  (id, name, country, logo_url, is_popular, created_at, updated_at)
VALUES
  ('10000000-0000-0000-0000-000000000001','BMW','DE',NULL,true,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000002','Mercedes-Benz','DE',NULL,true,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000003','Volkswagen','DE',NULL,true,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000004','Audi','DE',NULL,true,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000005','Toyota','JP',NULL,true,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000006','Honda','JP',NULL,true,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000007','Nissan','JP',NULL,false,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000008','Renault','FR',NULL,true,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000009','Peugeot','FR',NULL,true,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000010','SEAT','ES',NULL,true,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000011','Land Rover','GB',NULL,false,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000012','Ford','US',NULL,true,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000013','Tesla','US',NULL,true,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000014','Porsche','DE',NULL,false,NOW(),NOW()),
  ('10000000-0000-0000-0000-000000000015','Lexus','JP',NULL,false,NOW(),NOW())
ON CONFLICT (id) DO NOTHING;

-- ─── MODELOS ─────────────────────────────────────────────────────────────────
INSERT INTO vehicles.vehicle_models
  (id, make_id, name, body_types, created_at, updated_at)
VALUES
  ('20000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000001','Serie 3','Sedan',NOW(),NOW()),
  ('20000000-0000-0000-0000-000000000002','10000000-0000-0000-0000-000000000001','X5','Suv',NOW(),NOW()),
  ('20000000-0000-0000-0000-000000000003','10000000-0000-0000-0000-000000000002','Clase C','Sedan',NOW(),NOW()),
  ('20000000-0000-0000-0000-000000000004','10000000-0000-0000-0000-000000000002','GLE','Suv',NOW(),NOW()),
  ('20000000-0000-0000-0000-000000000005','10000000-0000-0000-0000-000000000003','Golf','Hatchback',NOW(),NOW()),
  ('20000000-0000-0000-0000-000000000006','10000000-0000-0000-0000-000000000004','A4','Sedan',NOW(),NOW()),
  ('20000000-0000-0000-0000-000000000007','10000000-0000-0000-0000-000000000005','Camry','Sedan',NOW(),NOW()),
  ('20000000-0000-0000-0000-000000000008','10000000-0000-0000-0000-000000000005','Land Cruiser','Suv',NOW(),NOW()),
  ('20000000-0000-0000-0000-000000000009','10000000-0000-0000-0000-000000000013','Model 3','Sedan',NOW(),NOW()),
  ('20000000-0000-0000-0000-000000000010','10000000-0000-0000-0000-000000000013','Model Y','Suv',NOW(),NOW())
ON CONFLICT (make_id, name) DO NOTHING;

-- ─── VEHÍCULOS (M2) ──────────────────────────────────────────────────────────
INSERT INTO vehicles.vehicles
  (id, slug, title, description_es, description_en,
   make_id, model_id, seller_id,
   year, mileage, condition, body_type, fuel_type, transmission,
   color, vin, price, currency, price_negotiable,
   country_origin, city, postal_code,
   status, is_featured, is_export_ready,
   specs, features,
   views_count, favorites_count, contacts_count,
   created_at, updated_at)
VALUES
  ('30000000-0000-0000-0000-000000000001',
   'bmw-serie-3-2021-munich',
   'BMW Serie 3 320d — Impecable, exportación lista',
   'BMW Serie 3 320d en perfecto estado. Revisiones al día, segunda mano con historial completo. Motor diésel 190 CV. Preparado para exportación a España o Francia.',
   'BMW 3 Series 320d in perfect condition. Full service history. Diesel 190hp. Ready for export.',
   '10000000-0000-0000-0000-000000000001','20000000-0000-0000-0000-000000000001',
   '00000000-0000-0000-0000-000000000003',
   2021,45000,'Used','Sedan','Diesel','Automatic','Azul Mediterráneo',
   'WBA5X9C03M9F45123',28500.00,'EUR',true,
   'DE','München','80331','Active',true,true,
   '{"potencia_cv":190,"par_nm":400,"cilindrada_cc":1998,"consumo_l100km":4.8}',
   '["Navegación","Asientos calefactados","Cámara trasera","Sensor aparcamiento","LED adaptativo"]',
   342,3,7,NOW() - INTERVAL '15 days',NOW()),

  ('30000000-0000-0000-0000-000000000002',
   'mercedes-gle-350-2020-berlin',
   'Mercedes GLE 350d 4MATIC — Todo terreno de lujo',
   'Mercedes GLE con tracción total 4MATIC, motor diésel 272 CV de bajo consumo. Techo panorámico, asientos de cuero Nappa. Perfectas condiciones.',
   'Mercedes GLE 350d 4MATIC. Panoramic roof, Nappa leather. Excellent condition.',
   '10000000-0000-0000-0000-000000000002','20000000-0000-0000-0000-000000000004',
   '00000000-0000-0000-0000-000000000003',
   2020,67000,'Used','Suv','Diesel','Automatic','Negro Obsidiana',
   'W1N1670131A456789',45000.00,'EUR',false,
   'DE','Berlin','10115','Active',false,true,
   '{"potencia_cv":272,"par_nm":600,"cilindrada_cc":2925,"consumo_l100km":6.5}',
   '["Techo panorámico","Cuero Nappa","Carplay","Asistente carril","Burmester audio"]',
   198,2,3,NOW() - INTERVAL '22 days',NOW()),

  ('30000000-0000-0000-0000-000000000003',
   'toyota-land-cruiser-200-2019-tokyo',
   'Toyota Land Cruiser 200 V8 — Importación directa Japón',
   'Land Cruiser 200 Serie con motor V8 4.6L de gasolina, 381 CV. Historial japonés completo (Toyota Japan). Ideal para importación a Marruecos y norte de África. Sin accidentes.',
   'Toyota Land Cruiser 200 V8 4.6L gasoline 381hp. Full Japanese service history. Perfect for Morocco and Africa. No accidents.',
   '10000000-0000-0000-0000-000000000005','20000000-0000-0000-0000-000000000008',
   '00000000-0000-0000-0000-000000000005',
   2019,85000,'Used','Suv','Gasoline','Automatic','Blanco Perla',
   'JTMHV05J594012345',52000.00,'EUR',true,
   'JP','Tokyo','100-0001','Active',true,true,
   '{"potencia_cv":381,"par_nm":530,"cilindrada_cc":4608,"consumo_l100km":12.4}',
   '["Tracción 4x4 permanente","Diferencial trasero","Cámara 360°","7 plazas","Suspensión adaptativa KDSS"]',
   521,5,15,NOW() - INTERVAL '8 days',NOW()),

  ('30000000-0000-0000-0000-000000000004',
   'tesla-model-y-2022-london',
   'Tesla Model Y Long Range — Cero emisiones, máximo confort',
   'Tesla Model Y Long Range con autonomía real de 533 km. Cargador doméstico incluido. Certificado para export a UE. Batería en perfecto estado (94% salud).',
   'Tesla Model Y Long Range. 533km real range. Home charger included. EU export certified. Battery health 94%.',
   '10000000-0000-0000-0000-000000000013','20000000-0000-0000-0000-000000000010',
   '00000000-0000-0000-0000-000000000007',
   2022,28000,'Used','Suv','Electric','Automatic','Azul Profundo',
   '5YJYGDEF2NF012345',42000.00,'EUR',false,
   'GB','London','SW1A 1AA','Active',false,true,
   '{"autonomia_km":533,"bateria_kwh":75,"potencia_cv":351,"carga_ac_kw":11,"carga_dc_kw":250}',
   '["Autopilot","Pantalla 15\"","Techo cristal","Supercharging compatible","Frunk"]',
   287,4,8,NOW() - INTERVAL '5 days',NOW()),

  ('30000000-0000-0000-0000-000000000005',
   'volkswagen-golf-8-2021-sevilla',
   'VW Golf 8 1.5 TSI — Como nuevo, primera mano',
   'Golf octava generación con motor 1.5 TSI evo de 130 CV y cambio manual. Un único propietario, cero incidencias. Mantenimiento en concesionario oficial.',
   'VW Golf 8 1.5 TSI 130hp. First owner, zero incidents. Official dealer maintenance.',
   '10000000-0000-0000-0000-000000000003','20000000-0000-0000-0000-000000000005',
   '00000000-0000-0000-0000-000000000002',
   2021,32000,'Used','Hatchback','Gasoline','Manual','Gris Platino',
   'WVWZZZE8ZME034567',23500.00,'EUR',true,
   'ES','Sevilla','41001','Active',false,false,
   '{"potencia_cv":130,"par_nm":200,"cilindrada_cc":1498,"consumo_l100km":5.2}',
   '["Lane assist","Frenado emergencia","Digital Cockpit","App-Connect","Climatizador"]',
   156,1,2,NOW() - INTERVAL '30 days',NOW()),

  ('30000000-0000-0000-0000-000000000006',
   'porsche-911-carrera-s-2022-hamburg',
   'Porsche 911 Carrera S — Deportivo de ensueño',
   'Porsche 911 992 Carrera S. Configuración exclusiva en Amarillo Racing con interiores negros. Opcionales Sport Chrono, PASM, frenos cerámicos. IVA deducible empresa. Libro de revisiones sellado.',
   'Porsche 911 992 Carrera S. Racing Yellow, black interior. Sport Chrono, PASM, ceramic brakes. VAT deductible.',
   '10000000-0000-0000-0000-000000000014',NULL,
   '00000000-0000-0000-0000-000000000003',
   2022,12000,'Used','Coupe','Gasoline','Automatic','Amarillo Racing',
   'WP0AB2A99NS223456',129000.00,'EUR',false,
   'DE','Hamburg','20095','Active',true,true,
   '{"potencia_cv":450,"par_nm":530,"cilindrada_cc":2981,"aceleracion_0_100":"3.5s","velocidad_max_kmh":308}',
   '["Sport Chrono","PASM","Frenos cerámica","Escapes Sport","Bose Surround"]',
   678,8,12,NOW() - INTERVAL '3 days',NOW()),

  ('30000000-0000-0000-0000-000000000007',
   'renault-megane-e-tech-2020-paris',
   'Renault Mégane E-Tech PHEV 160 — Híbrido enchufable',
   'Mégane híbrido enchufable con 50 km de autonomía eléctrica pura. Batería nueva (2023). Ideal para ciudad y trayectos mixtos.',
   'Renault Megane E-Tech PHEV. 50km electric range. New battery (2023). City & mixed use.',
   '10000000-0000-0000-0000-000000000008',NULL,
   '00000000-0000-0000-0000-000000000006',
   2020,41000,'Used','Hatchback','PluginHybrid','Automatic','Rojo Fuego',
   'VF1RFB00066512345',18900.00,'EUR',true,
   'FR','Paris','75001','Reviewing',false,false,
   '{"potencia_cv":160,"autonomia_electrica_km":50,"bateria_kwh":9.8}',
   '["Carga rápida AC","Carplay","Climatizador bizona","Sensores parking"]',
   12,0,0,NOW() - INTERVAL '1 days',NOW()),

  ('30000000-0000-0000-0000-000000000008',
   'audi-a4-2019-madrid-vendido',
   'Audi A4 2.0 TDI 150 CV — VENDIDO',
   'Audi A4 sedan 2.0 TDI 150 CV en perfecto estado. Motor diésel económico, caja S-Tronic.',
   'Audi A4 2.0 TDI 150hp. Economy diesel, S-Tronic gearbox.',
   '10000000-0000-0000-0000-000000000004','20000000-0000-0000-0000-000000000006',
   '00000000-0000-0000-0000-000000000002',
   2019,78000,'Used','Sedan','Diesel','Automatic','Gris Daytona',
   'WAUZZZ8K5KA098765',22000.00,'EUR',false,
   'ES','Madrid','28001','Sold',false,false,
   '{"potencia_cv":150,"par_nm":320,"cilindrada_cc":1968}',
   '["Navegación MMI","Asientos deportivos","Sensor lluvia","Arranque sin llave"]',
   432,2,8,NOW() - INTERVAL '60 days',NOW() - INTERVAL '10 days'),

  ('30000000-0000-0000-0000-000000000009',
   'honda-civic-2018-tokyo',
   'Honda Civic Type R 2018 — JDM Turbo',
   'Honda Civic Type R de importación japonesa directa (JDM). Motor VTEC Turbo 320 CV. Homologación UE disponible en nuestro taller.',
   'Honda Civic Type R JDM. VTEC Turbo 320hp. EU homologation available.',
   '10000000-0000-0000-0000-000000000006',NULL,
   '00000000-0000-0000-0000-000000000005',
   2018,62000,'Used','Hatchback','Gasoline','Manual','Rojo Rallye',
   'JHMFK7H49JX000789',28000.00,'EUR',true,
   'JP','Tokyo','150-0001','Active',false,true,
   '{"potencia_cv":320,"par_nm":400,"cilindrada_cc":1996,"aceleracion_0_100":"5.8s"}',
   '["Brembo frenos","Asientos Recaro","Data Logger","Diferencial LSD adaptativo"]',
   189,7,4,NOW() - INTERVAL '12 days',NOW()),

  ('30000000-0000-0000-0000-000000000010',
   'lexus-lc-500-2021-london',
   'Lexus LC 500 V8 — Gran Turismo japonés',
   'Lexus LC 500 con motor V8 atmosférico de 477 CV. El gran turismo japonés por excelencia. Mantenimiento oficial Lexus UK.',
   'Lexus LC 500 V8 naturally aspirated 477hp. The Japanese grand tourer. Full Lexus UK history.',
   '10000000-0000-0000-0000-000000000015',NULL,
   '00000000-0000-0000-0000-000000000007',
   2021,19000,'Used','Coupe','Gasoline','Automatic','Azul Structural',
   'JTHDZ5BH4M5010234',78000.00,'EUR',false,
   'GB','London','EC2A 1AA','Active',true,true,
   '{"potencia_cv":477,"par_nm":540,"cilindrada_cc":4969,"aceleracion_0_100":"4.7s"}',
   '["Mark Levinson audio","Head-up display","Asientos ventilados","Techo cristal"]',
   234,6,5,NOW() - INTERVAL '7 days',NOW())
ON CONFLICT (id) DO NOTHING;

-- ─── IMÁGENES DE VEHÍCULOS ──────────────────────────────────────────────────
INSERT INTO vehicles.vehicle_images
  (id, vehicle_id, url, thumbnail_url, is_primary, sort_order, alt_text, created_at, updated_at)
VALUES
  ('40000000-0000-0000-0000-000000000001','30000000-0000-0000-0000-000000000001',
   '/uploads/vehicles/bmw-s3-front.jpg','/uploads/vehicles/bmw-s3-front-thumb.jpg',
   true,0,'BMW Serie 3 frontal',NOW(),NOW()),
  ('40000000-0000-0000-0000-000000000002','30000000-0000-0000-0000-000000000001',
   '/uploads/vehicles/bmw-s3-lateral.jpg','/uploads/vehicles/bmw-s3-lateral-thumb.jpg',
   false,1,'BMW Serie 3 lateral',NOW(),NOW()),
  ('40000000-0000-0000-0000-000000000003','30000000-0000-0000-0000-000000000002',
   '/uploads/vehicles/gle-front.jpg','/uploads/vehicles/gle-front-thumb.jpg',
   true,0,'Mercedes GLE frontal',NOW(),NOW()),
  ('40000000-0000-0000-0000-000000000004','30000000-0000-0000-0000-000000000002',
   '/uploads/vehicles/gle-interior.jpg','/uploads/vehicles/gle-interior-thumb.jpg',
   false,1,'Mercedes GLE interior',NOW(),NOW()),
  ('40000000-0000-0000-0000-000000000005','30000000-0000-0000-0000-000000000003',
   '/uploads/vehicles/landcruiser-front.jpg','/uploads/vehicles/landcruiser-thumb.jpg',
   true,0,'Toyota Land Cruiser frontal',NOW(),NOW()),
  ('40000000-0000-0000-0000-000000000006','30000000-0000-0000-0000-000000000003',
   '/uploads/vehicles/landcruiser-rear.jpg','/uploads/vehicles/landcruiser-rear-thumb.jpg',
   false,1,'Toyota Land Cruiser trasera',NOW(),NOW()),
  ('40000000-0000-0000-0000-000000000007','30000000-0000-0000-0000-000000000004',
   '/uploads/vehicles/tesla-my-blue.jpg','/uploads/vehicles/tesla-my-thumb.jpg',
   true,0,'Tesla Model Y azul',NOW(),NOW()),
  ('40000000-0000-0000-0000-000000000008','30000000-0000-0000-0000-000000000005',
   '/uploads/vehicles/golf8-grey.jpg','/uploads/vehicles/golf8-thumb.jpg',
   true,0,'VW Golf 8 gris',NOW(),NOW()),
  ('40000000-0000-0000-0000-000000000009','30000000-0000-0000-0000-000000000006',
   '/uploads/vehicles/porsche-yellow.jpg','/uploads/vehicles/porsche-thumb.jpg',
   true,0,'Porsche 911 amarillo',NOW(),NOW()),
  ('40000000-0000-0000-0000-000000000010','30000000-0000-0000-0000-000000000006',
   '/uploads/vehicles/porsche-rear.jpg','/uploads/vehicles/porsche-rear-thumb.jpg',
   false,1,'Porsche 911 trasera',NOW(),NOW()),
  ('40000000-0000-0000-0000-000000000011','30000000-0000-0000-0000-000000000009',
   '/uploads/vehicles/civic-type-r.jpg','/uploads/vehicles/civic-type-r-thumb.jpg',
   true,0,'Honda Civic Type R rojo',NOW(),NOW()),
  ('40000000-0000-0000-0000-000000000012','30000000-0000-0000-0000-000000000010',
   '/uploads/vehicles/lexus-lc500.jpg','/uploads/vehicles/lexus-lc500-thumb.jpg',
   true,0,'Lexus LC500 azul',NOW(),NOW())
ON CONFLICT (id) DO NOTHING;

-- ─── FAVORITOS ──────────────────────────────────────────────────────────────
INSERT INTO vehicles.saved_vehicles
  (id, user_id, vehicle_id, created_at, updated_at)
VALUES
  ('50000000-0000-0000-0000-000000000001',
   '00000000-0000-0000-0000-000000000004',
   '30000000-0000-0000-0000-000000000003',NOW() - INTERVAL '5 days',NOW() - INTERVAL '5 days'),
  ('50000000-0000-0000-0000-000000000002',
   '00000000-0000-0000-0000-000000000004',
   '30000000-0000-0000-0000-000000000001',NOW() - INTERVAL '3 days',NOW() - INTERVAL '3 days'),
  ('50000000-0000-0000-0000-000000000003',
   '00000000-0000-0000-0000-000000000006',
   '30000000-0000-0000-0000-000000000006',NOW() - INTERVAL '2 days',NOW() - INTERVAL '2 days'),
  ('50000000-0000-0000-0000-000000000004',
   '00000000-0000-0000-0000-000000000004',
   '30000000-0000-0000-0000-000000000009',NOW() - INTERVAL '1 day',NOW() - INTERVAL '1 day'),
  ('50000000-0000-0000-0000-000000000005',
   '00000000-0000-0000-0000-000000000006',
   '30000000-0000-0000-0000-000000000004',NOW() - INTERVAL '4 days',NOW() - INTERVAL '4 days')
ON CONFLICT (id) DO NOTHING;

-- ─── CONVERSACIONES Y MENSAJES (M5) ─────────────────────────────────────────
INSERT INTO messaging.conversations
  (id, buyer_id, seller_id, vehicle_id,
   is_archived_by_buyer, is_archived_by_seller,
   last_message_at, created_at, updated_at)
VALUES
  ('60000000-0000-0000-0000-000000000001',
   '00000000-0000-0000-0000-000000000004',
   '00000000-0000-0000-0000-000000000005',
   '30000000-0000-0000-0000-000000000003',
   false,false,NOW() - INTERVAL '2 hours',
   NOW() - INTERVAL '1 day',NOW() - INTERVAL '2 hours'),
  ('60000000-0000-0000-0000-000000000002',
   '00000000-0000-0000-0000-000000000006',
   '00000000-0000-0000-0000-000000000003',
   '30000000-0000-0000-0000-000000000001',
   false,false,NOW() - INTERVAL '30 minutes',
   NOW() - INTERVAL '3 days',NOW() - INTERVAL '30 minutes'),
  ('60000000-0000-0000-0000-000000000003',
   '00000000-0000-0000-0000-000000000004',
   '00000000-0000-0000-0000-000000000007',
   '30000000-0000-0000-0000-000000000004',
   false,false,NOW() - INTERVAL '6 hours',
   NOW() - INTERVAL '2 days',NOW() - INTERVAL '6 hours')
ON CONFLICT (buyer_id, seller_id, vehicle_id) DO NOTHING;

INSERT INTO messaging.messages
  (id, conversation_id, sender_id, body, is_read, read_at, created_at, updated_at)
VALUES
  -- Conv 1: Youssef pregunta por Land Cruiser a Hiroshi
  ('61000000-0000-0000-0000-000000000001',
   '60000000-0000-0000-0000-000000000001',
   '00000000-0000-0000-0000-000000000004',
   'Bonjour Hiroshi! Je suis très intéressé par votre Land Cruiser. Est-il disponible pour export vers le Maroc? Quel est le prix final avec les frais?',
   true,NOW() - INTERVAL '20 hours',NOW() - INTERVAL '23 hours',NOW() - INTERVAL '23 hours'),
  ('61000000-0000-0000-0000-000000000002',
   '60000000-0000-0000-0000-000000000001',
   '00000000-0000-0000-0000-000000000005',
   'Bonjour Youssef! Oui, le véhicule est disponible pour export. Tous les documents sont en ordre pour la douane marocaine. Le prix de 52.000€ est négociable si vous prenez une décision rapidement.',
   true,NOW() - INTERVAL '18 hours',NOW() - INTERVAL '20 hours',NOW() - INTERVAL '20 hours'),
  ('61000000-0000-0000-0000-000000000003',
   '60000000-0000-0000-0000-000000000001',
   '00000000-0000-0000-0000-000000000004',
   'Merci! Pouvez-vous me donner des détails sur les frais de dédouanement et d''homologation pour le Maroc? Est-ce que vous avez déjà exporté vers le Maroc?',
   false,NULL,NOW() - INTERVAL '2 hours',NOW() - INTERVAL '2 hours'),

  -- Conv 2: Marie pregunta por BMW a Hans
  ('61000000-0000-0000-0000-000000000004',
   '60000000-0000-0000-0000-000000000002',
   '00000000-0000-0000-0000-000000000006',
   'Bonjour! J''ai vu votre BMW Série 3 sur Logistique Les Lions. Le prix de 28.500€ est-il négociable? Je suis à Paris, est-ce que vous pouvez organiser le transport?',
   true,NOW() - INTERVAL '2 days',NOW() - INTERVAL '3 days',NOW() - INTERVAL '3 days'),
  ('61000000-0000-0000-0000-000000000005',
   '60000000-0000-0000-0000-000000000002',
   '00000000-0000-0000-0000-000000000003',
   'Hallo Marie! Ja, für 27.000€ ist es möglich. Der Transport nach Paris kostet ca. 800€ extra mit einem Fahrzeugtransporter. Ich kann auch die Papiere für Frankreich vorbereiten.',
   true,NOW() - INTERVAL '1 day',NOW() - INTERVAL '2 days',NOW() - INTERVAL '2 days'),
  ('61000000-0000-0000-0000-000000000006',
   '60000000-0000-0000-0000-000000000002',
   '00000000-0000-0000-0000-000000000006',
   'Parfait! 27.000€ + transport c''est acceptable. Quels documents dois-je préparer de mon côté pour l''immatriculation en France?',
   false,NULL,NOW() - INTERVAL '30 minutes',NOW() - INTERVAL '30 minutes'),

  -- Conv 3: Youssef pregunta por Tesla a James
  ('61000000-0000-0000-0000-000000000007',
   '60000000-0000-0000-0000-000000000003',
   '00000000-0000-0000-0000-000000000004',
   'Hello James! Very interested in the Tesla Model Y. Can you confirm the battery health percentage and if it has been in any accidents? Also, is the price negotiable for €40.000?',
   true,NOW() - INTERVAL '1 day',NOW() - INTERVAL '2 days',NOW() - INTERVAL '2 days'),
  ('61000000-0000-0000-0000-000000000008',
   '60000000-0000-0000-0000-000000000003',
   '00000000-0000-0000-0000-000000000007',
   'Hi Youssef! Battery health is confirmed at 94% (Tesla app report available). Zero accidents, one previous owner. I can go down to €41.000 including the home charger. All export documents ready.',
   true,NOW() - INTERVAL '18 hours',NOW() - INTERVAL '1 day',NOW() - INTERVAL '1 day'),
  ('61000000-0000-0000-0000-000000000009',
   '60000000-0000-0000-0000-000000000003',
   '00000000-0000-0000-0000-000000000004',
   'Deal at €41.000. How do we proceed with the payment and paperwork? I would prefer to use the escrow service on this platform.',
   false,NULL,NOW() - INTERVAL '6 hours',NOW() - INTERVAL '6 hours')
ON CONFLICT (id) DO NOTHING;

-- ─── TRAMITACIÓN / COMPLIANCE (M3) ──────────────────────────────────────────
-- Los datos de país ya existen de la migración de init.sql/CountryConfiguration
-- Solo añadimos si no existen
INSERT INTO compliance.country_requirements
  (id, origin_country, destination_country,
   document_types_json, homologation_required,
   customs_rate_percent, vat_rate_percent,
   technical_inspection_required,
   estimated_processing_cost_eur, estimated_days,
   notes_es, notes_en, last_updated_at, created_at, updated_at)
VALUES
  ('70000000-0000-0000-0000-000000000001','DE','ES',
   '["Título propiedad","ITV alemana","Certificado conformidad UE","Factura compraventa","DNI/Pasaporte"]',
   false,0.0,21.0,true,850.0,15,
   'Dentro de la UE no hay aranceles. Solo pagar IVA español y homologar si aplica.',
   'Within EU no customs duties. Only Spanish VAT and homologation if applicable.',
   NOW(),NOW(),NOW()),
  ('70000000-0000-0000-0000-000000000002','JP','ES',
   '["Título propiedad japonés","Certificado exportación Japón","Certificado conformidad","Bill of Lading","Factura comercial","Packing list"]',
   true,6.5,21.0,true,3200.0,45,
   'Importación extra-UE. Arancel 6.5%. Homologación obligatoria en ITV.',
   'Extra-EU import. 6.5% customs tariff. Mandatory homologation at ITV.',
   NOW(),NOW(),NOW()),
  ('70000000-0000-0000-0000-000000000003','ES','MA',
   '["Pasaporte","Título propiedad","Certificado no adeudo","TK3 aduanas","Contrato compraventa"]',
   false,25.0,20.0,false,1200.0,30,
   'Marruecos aplica arancel del 25%. IVA marroquí 20%.',
   'Morocco applies 25% tariff. Moroccan VAT 20%.',
   NOW(),NOW(),NOW()),
  ('70000000-0000-0000-0000-000000000004','DE','FR',
   '["Carte grise alemana","Certificado conformidad CE","Factura","Certificado contrôle technique"]',
   false,0.0,20.0,true,650.0,10,
   'Dentro de la UE, sin aranceles. IVA francés 20%.',
   'Within EU, no customs. French VAT 20%.',
   NOW(),NOW(),NOW()),
  ('70000000-0000-0000-0000-000000000005','ES','GB',
   '["V5C equivalente","Certificado conformidad UK","MOT test","DVLA notification","Insurance proof"]',
   true,6.5,20.0,true,1800.0,25,
   'Post-Brexit: arancel 6.5% desde España a UK. MOT obligatorio.',
   'Post-Brexit: 6.5% tariff from Spain to UK. MOT required.',
   NOW(),NOW(),NOW()),
  ('70000000-0000-0000-0000-000000000006','US','ES',
   '["Title certificate USA","Export certificate NHTSA","Bill of Lading","Homologación UE","Factura comercial"]',
   true,6.5,21.0,true,4500.0,60,
   'Importación USA muy compleja. Homologación UE costosa.',
   'USA import is complex. EU homologation expensive.',
   NOW(),NOW(),NOW()),
  ('70000000-0000-0000-0000-000000000007','JP','MA',
   '["Titre de propriété japonais","Certificat export Japon","Bill of Lading","Déclaration douane ADII","Facture commerciale"]',
   false,25.0,20.0,false,2800.0,50,
   'Importación Japón→Marruecos. Alto arancel marroquí. Sin homologación especial.',
   'Japan to Morocco import. High Moroccan tariff. No special homologation.',
   NOW(),NOW(),NOW()),
  ('70000000-0000-0000-0000-000000000008','GB','ES',
   '["V5C","Certificado conformidad CE","Carta verde seguro","Factura compraventa"]',
   false,6.5,21.0,true,1200.0,20,
   'Post-Brexit: UK→ES tiene arancel 6.5%. Certificado conformidad europeo requerido.',
   'Post-Brexit: UK to ES 6.5% tariff. European conformity certificate required.',
   NOW(),NOW(),NOW())
ON CONFLICT (origin_country, destination_country) DO NOTHING;

-- ─── PROCESOS DE IMPORTACIÓN ACTIVOS ─────────────────────────────────────────
INSERT INTO compliance.import_export_processes
  (id, buyer_id, seller_id, vehicle_id,
   origin_country, destination_country,
   process_type, status, estimated_cost_eur,
   completion_percent, started_at, created_at, updated_at)
VALUES
  ('80000000-0000-0000-0000-000000000001',
   '00000000-0000-0000-0000-000000000004',
   '00000000-0000-0000-0000-000000000005',
   '30000000-0000-0000-0000-000000000003',
   'JP','MA','ImportExtraEu','InProgress',3500.0,35,
   NOW() - INTERVAL '10 days',NOW() - INTERVAL '10 days',NOW()),
  ('80000000-0000-0000-0000-000000000002',
   '00000000-0000-0000-0000-000000000006',
   '00000000-0000-0000-0000-000000000003',
   '30000000-0000-0000-0000-000000000001',
   'DE','FR','IntraEu','PendingDocuments',700.0,60,
   NOW() - INTERVAL '5 days',NOW() - INTERVAL '5 days',NOW()),
  ('80000000-0000-0000-0000-000000000003',
   '00000000-0000-0000-0000-000000000004',
   '00000000-0000-0000-0000-000000000002',
   '30000000-0000-0000-0000-000000000008',
   'ES','MA','ExportExtraEu','Completed',1300.0,100,
   NOW() - INTERVAL '40 days',NOW() - INTERVAL '40 days',NOW() - INTERVAL '5 days'),
  ('80000000-0000-0000-0000-000000000004',
   '00000000-0000-0000-0000-000000000004',
   '00000000-0000-0000-0000-000000000007',
   '30000000-0000-0000-0000-000000000004',
   'GB','MA','ImportExtraEu','UnderReview',2200.0,75,
   NOW() - INTERVAL '7 days',NOW() - INTERVAL '7 days',NOW())
ON CONFLICT (id) DO NOTHING;

-- ─── PLANTILLAS DE DOCUMENTOS ────────────────────────────────────────────────
INSERT INTO compliance.document_templates
  (id, country, document_type,
   template_url, instructions_es, instructions_en,
   official_url, issuing_authority,
   estimated_cost_eur, estimated_days,
   created_at, updated_at)
VALUES
  ('90000000-0000-0000-0000-000000000001','ES','CertificadoConformidad',
   '/templates/cert-conformidad-ue.pdf',
   'Solicitar en la marca del vehículo o distribuidor oficial. Necesario para matriculación.',
   'Request from vehicle brand or official dealer. Required for registration.',
   'https://ec.europa.eu/growth/sectors/automotive','Fabricante / Marca',50.0,7,NOW(),NOW()),
  ('90000000-0000-0000-0000-000000000002','ES','SolicitudMatriculacion',
   '/templates/modelo-576-es.pdf',
   'Modelo 576 de la Agencia Tributaria para el Impuesto de Matriculación.',
   'Form 576 from Spanish Tax Agency for vehicle registration tax.',
   'https://sede.agenciatributaria.gob.es','Agencia Tributaria España',0.0,1,NOW(),NOW()),
  ('90000000-0000-0000-0000-000000000003','MA','DeclaracionAduanera',
   '/templates/declaracion-aduana-ma.pdf',
   'Formulario oficial de la ADII para importación de vehículos. Presentar en aduana de entrada.',
   'Official ADII form for vehicle import. Present at entry customs.',
   'https://www.douane.gov.ma','ADII Marruecos',0.0,1,NOW(),NOW()),
  ('90000000-0000-0000-0000-000000000004','JP','ExportCertificate',
   '/templates/deregistration-jp.pdf',
   'Certificado de baja en el registro japonés (shakken). Imprescindible para exportar.',
   'De-registration certificate from Japanese vehicle registry. Mandatory for export.',
   'https://www.mlit.go.jp','MLIT Japan',80.0,5,NOW(),NOW()),
  ('90000000-0000-0000-0000-000000000005','FR','CartreGrise',
   '/templates/carte-grise-fr.pdf',
   'Certificat d''immatriculation français. Se obtiene online en ANTS.',
   'French vehicle registration certificate. Obtained online at ANTS.',
   'https://www.ants.gouv.fr','ANTS France',11.0,10,NOW(),NOW())
ON CONFLICT (id) DO NOTHING;

-- ─── ARANCELES ───────────────────────────────────────────────────────────────
INSERT INTO compliance.customs_tariffs
  (id, origin_country, destination_country, hs_code,
   tariff_rate_percent, valid_from, source,
   created_at, updated_at)
VALUES
  ('a0000000-0000-0000-0000-000000000001','JP','ES','8703.23',
   6.5,'2021-01-01',
   'Arancel TEC UE para turismos > 1.500cc procedentes de Japón',
   NOW(),NOW()),
  ('a0000000-0000-0000-0000-000000000002','US','ES','8703.23',
   6.5,'2021-01-01',
   'Arancel TEC UE para turismos > 1.500cc procedentes de USA',
   NOW(),NOW()),
  ('a0000000-0000-0000-0000-000000000003','ES','MA','8703.23',
   25.0,'2020-01-01',
   'Arancel marroquí Accord d''Association EU-Maroc para vehículos de ocasión',
   NOW(),NOW()),
  ('a0000000-0000-0000-0000-000000000004','JP','ES','8703.80',
   0.0,'2023-01-01',
   'Vehículos eléctricos exentos según Reg. UE 2022/2078 (EV Green Lane)',
   NOW(),NOW()),
  ('a0000000-0000-0000-0000-000000000005','GB','ES','8703.23',
   6.5,'2021-01-01',
   'Post-Brexit TCA arancel UK→UE para turismos',
   NOW(),NOW()),
  ('a0000000-0000-0000-0000-000000000006','DE','ES','8703.23',
   0.0,'2020-01-01',
   'Libre circulación UE: sin arancel entre estados miembro',
   NOW(),NOW())
ON CONFLICT (id) DO NOTHING;

-- ─── REQUISITOS DE HOMOLOGACIÓN ───────────────────────────────────────────────
INSERT INTO compliance.homologation_requirements
  (id, destination_country, vehicle_category,
   year_from, year_to, emission_standard,
   required_modifications, estimated_cost_eur,
   certifying_body, notes_es, created_at, updated_at)
VALUES
  ('b0000000-0000-0000-0000-000000000001','ES','M1',
   2000,NULL,'Euro 6',
   '["Conversión de luces a norma europea","Test de emisiones Euro 6","Adaptación OBD"]',
   1800.0,'ITV España',
   'Vehículos JDM japoneses requieren conversión de luces a norma europea y test de emisiones Euro 6.',
   NOW(),NOW()),
  ('b0000000-0000-0000-0000-000000000002','ES','M1',
   1995,2005,'Euro 4',
   '["Conversión luces","Adaptación parachoques","Instalación OBD2","Emisiones Euro 4"]',
   3500.0,'ITV + Taller homologado',
   'Vehículos americanos (Federal spec) requieren conversión completa. Costoso pero factible.',
   NOW(),NOW()),
  ('b0000000-0000-0000-0000-000000000003','ES','M1',
   2021,NULL,'Euro 6d',
   '["Certificado conformidad UK equivalente CE"]',
   500.0,'ITV España',
   'Post-Brexit UK→ES: solo nuevo CoC europeo. Sin modificaciones físicas si el vehículo ya cumple Euro 6d.',
   NOW(),NOW())
ON CONFLICT (id) DO NOTHING;

-- ─── NEWSLETTER ──────────────────────────────────────────────────────────────
INSERT INTO vehicles.newsletter_subscribers
  (id, email, subscribed_at, is_active)
VALUES
  ('c0000000-0000-0000-0000-000000000001','youssef.benali@hotmail.com',NOW() - INTERVAL '20 days',true),
  ('c0000000-0000-0000-0000-000000000002','marie.dupont@orange.fr',NOW() - INTERVAL '15 days',true),
  ('c0000000-0000-0000-0000-000000000003','info@autoiberia.de',NOW() - INTERVAL '7 days',true),
  ('c0000000-0000-0000-0000-000000000004','unsubscribed@test.com',NOW() - INTERVAL '30 days',false),
  ('c0000000-0000-0000-0000-000000000005','hans.mueller@gmail.de',NOW() - INTERVAL '45 days',true)
ON CONFLICT (id) DO NOTHING;

-- ─── ACTUALIZAR CONTADORES DENORMALIZADOS ────────────────────────────────────
UPDATE vehicles.vehicles v
SET favorites_count = (
  SELECT COUNT(*) FROM vehicles.saved_vehicles sv WHERE sv.vehicle_id = v.id
);

COMMIT;

-- ─── VERIFICACIÓN FINAL ──────────────────────────────────────────────────────
SELECT 'usuarios'           AS tabla, COUNT(*) AS registros FROM users.user_profiles
UNION ALL SELECT 'marcas',              COUNT(*) FROM vehicles.vehicle_makes
UNION ALL SELECT 'modelos',             COUNT(*) FROM vehicles.vehicle_models
UNION ALL SELECT 'vehículos',           COUNT(*) FROM vehicles.vehicles
UNION ALL SELECT 'imágenes',            COUNT(*) FROM vehicles.vehicle_images
UNION ALL SELECT 'favoritos',           COUNT(*) FROM vehicles.saved_vehicles
UNION ALL SELECT 'conversaciones',      COUNT(*) FROM messaging.conversations
UNION ALL SELECT 'mensajes',            COUNT(*) FROM messaging.messages
UNION ALL SELECT 'requisitos país',     COUNT(*) FROM compliance.country_requirements
UNION ALL SELECT 'procesos tramitación',COUNT(*) FROM compliance.import_export_processes
UNION ALL SELECT 'plantillas doc',      COUNT(*) FROM compliance.document_templates
UNION ALL SELECT 'aranceles',           COUNT(*) FROM compliance.customs_tariffs
UNION ALL SELECT 'homologaciones',      COUNT(*) FROM compliance.homologation_requirements
UNION ALL SELECT 'newsletter',          COUNT(*) FROM vehicles.newsletter_subscribers
ORDER BY tabla;

