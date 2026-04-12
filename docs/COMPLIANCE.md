# Módulo de Tramitación Internacional — Documentación Detallada

## Introducción

El módulo de Compliance es el **diferenciador principal** de la plataforma. Mientras competidores como coches.net o AutoScout24 solo facilitan el contacto entre compradores y vendedores, Logistique Les Lions gestiona todo el papeleo transfronterizo.

---

## Tipos de Proceso

### 1. Intra-UE (ej: Alemania → España)

Misma zona aduanera. No hay aranceles, pero sí requisitos de matriculación.

**Documentos requeridos:**

| Documento | Responsable | Plazo típico | Coste estimado |
|---|---|---|---|
| Factura de compraventa | Vendedor + comprador | Inmediato | 0 € |
| Ficha técnica (Zulassungsbescheinigung) | Vendedor | Disponible en venta | 0 € |
| Traducción jurada ficha técnica | Comprador | 3-5 días | 50-100 € |
| Certificado de Conformidad (COC) | Fabricante/Vendedor | Disponible si original | 0-300 € |
| Baja temporal de circulación | Vendedor (país origen) | 1-2 días | 0-30 € |
| Prueba de pago del IVA intracomunitario | Vendedor | Inmediato | 0 € |
| Seguro temporal europeo | Comprador | Inmediato | 50-150 € |
| ITV española (inspección técnica) | Comprador | 1-3 días | 40-70 € |
| Formulario 576 (impuesto matriculación) | Comprador | En Hacienda | % precio |
| Alta en Tráfico (matrícula española) | Comprador | 1-5 días | 100-200 € |

**Impuestos intra-UE España:**
- IVA: exento si vendedor es empresa (operación intracomunitaria) → comprador liquida IVA en España
- IEDMT (Impuesto Especial sobre Determinados Medios de Transporte):
  - Emisiones <120g CO2: 0%
  - 120-160g: 4.75%
  - 160-200g: 9.75%
  - >200g: 14.75%

**Tiempo total estimado:** 2-4 semanas

---

### 2. Importación Extra-UE → España (ej: USA o Japón → España)

Cruza la frontera aduanera de la UE. Aranceles + IVA a la importación + IEDMT.

**Documentos requeridos:**

| Documento | Responsable | Plazo típico | Coste estimado |
|---|---|---|---|
| Título de propiedad (Title) apostillado | Vendedor | 5-15 días | 50-200 € |
| Certificado de no robo (notarizado) | Vendedor | 3-7 días | 100-300 € |
| Factura de compraventa comercial | Vendedor + comprador | Inmediato | 0 € |
| Traducción jurada de todos los docs | Comprador | 3-5 días | 150-400 € |
| Declaración Aduanera DUA (aduana UE) | Agente aduanero | 1-3 días | 200-500 € |
| Certificado de origen del vehículo | Vendedor | Disponible | 0 € |
| Pago aranceles UE (10%) | Comprador | En aduana | 10% valor |
| Pago IVA importación (21% España) | Comprador | Con DUA | 21% (valor + arancel) |
| Homologación individual (o por tipos) | Comprador | 1-6 meses | 500-3000 € |
| Modificaciones técnicas obligatorias | Taller certificado | Variable | 200-2000 € |
| ITV reforzada post-homologación | Estación ITV | 1-5 días | 70-150 € |
| IEDMT | Hacienda | Con matriculación | % precio |
| Alta en Tráfico | Comprador | 1-5 días | 100-200 € |

**Modificaciones técnicas habituales (USA → UE):**
- Conversión faros (USA usa amarillo, UE blanco)
- Limitador de velocidad
- Adaptación airbags (normativa UNECE R94)
- OBD-II → EOBD (normativas emisiones)
- Símbolo velocímetro (mph → km/h)
- Adaptación retrovisores

**Cálculo de coste total importación USA → España (ejemplo 30.000 USD):**
```
Precio vehículo (CIF):          30.000 €
Arancel UE 10%:                  3.000 €
Base imponible IVA:             33.000 €
IVA 21%:                         6.930 €
IEDMT (según emisiones):         0-5% precio
Homologación:                  500-3.000 €
Modificaciones:               500-2.000 €
Agente aduanero:               200-500 €
Transporte:                   800-1.500 €
─────────────────────────────────────────
COSTE TOTAL ESTIMADO:      41.930 - 49.930 €
```

**Tiempo total estimado:** 3-6 meses (homologación individual puede alargarse)

---

### 3. Exportación España → Marruecos (Extra-UE salida)

**Documentos requeridos:**

| Documento | País | Responsable |
|---|---|---|
| Declaración de exportación EX1 | España (Aduana) | Exportador/agente |
| Baja definitiva española (Permisos de circulación) | España (DGT) | Vendedor |
| Certificado de libre venta | España (notarial) | Vendedor |
| Factura comercial bilingüe (ES + AR) | Ambos | Vendedor + comprador |
| Packing list | Exportador | Vendedor |
| Contrato de compraventa apostillado | Ambos | Ambos |
| Documentación aduanera marroquí (importación MA) | Marruecos | Comprador MA |
| Pago derechos de importación Marruecos | Marruecos | Comprador MA |
| Certificado de verificación técnica marroquí | Marruecos | Comprador MA |
| Seguro obligatorio marroquí | Marruecos | Comprador MA |

**Aranceles Marruecos:**
- Vehículos UE: 17.5% (Acuerdo de Asociación UE-Marruecos)
- Vehículos nuevos (matriculados <6 meses): 2.5% adicional
- TVA Marruecos: 20%

---

## Tabla de Países y Normativas

| País | Zona | Import desde UE | Export hacia | IVA | Homologación |
|---|---|---|---|---|---|
| 🇩🇪 Alemania | UE | Intra-UE | Intra-UE | 19% | COC obligatorio |
| 🇫🇷 Francia | UE | Intra-UE | Intra-UE | 20% | COC obligatorio |
| 🇪🇸 España | UE | Intra-UE | Intra-UE | 21% | COC + IEDMT |
| 🇮🇹 Italia | UE | Intra-UE | Intra-UE | 22% | COC obligatorio |
| 🇬🇧 UK | Post-Brexit | Extra-UE | Extra-UE | 20% | DVLA individual |
| 🇺🇸 USA | Extra-UE | Extra-UE (import) | — | — | Homologación UE oblig. |
| 🇲🇦 Marruecos | Extra-UE | — | Extra-UE (export) | 20% | Verificación MA |
| 🇯🇵 Japón | Extra-UE | Extra-UE (import) | — | — | Homologación UE oblig. |
| 🇨🇭 Suiza | No-UE | Bilat. CH-UE | Bilat. CH-UE | 7.7% | MOFIS certificate |

---

## Estructura de Datos (Base de Datos)

### Tabla `compliance.import_export_processes`
Proceso completo de importación/exportación asociado a una transacción de vehículo.

### Tabla `compliance.process_documents`
Cada documento del checklist con su estado (pending/uploaded/verified/rejected/expired).

### Tabla `compliance.country_requirements`
Normativa actualizable por par (origen, destino) con lista de document_types requeridos y costes.

### Tabla `compliance.homologation_requirements`
Requisitos técnicos por categoría de vehículo y país destino.

### Tabla `compliance.customs_tariffs`
Tasas aduaneras por código HS y par de países.

---

## Flujo de un Proceso Típico

```
1. Comprador selecciona vehículo → elige "Solicitar importación"
2. Sistema detecta par origen/destino → consulta country_requirements
3. Se crea ImportExportProcess con checklist automático de documentos
4. Vendedor y comprador reciben notificaciones con sus responsabilidades
5. Documentos se van subiendo → sistema verifica formato/validez
6. Si todo OK → proceso avanza al siguiente estado
7. Sistema notifica con 15 días de antelación los documentos que caducan
8. Al completarse → se libera el escrow y se transfiere el vehículo
```

---

## Partners de Homologación (a integrar)

| País | Organismo | API disponible |
|---|---|---|
| España | IDIADA / APPLUS | Sí (REST) |
| Alemania | DEKRA / TÜV | Parcial |
| Francia | Utac | No (manual) |
| UK | DVSA | Sí |

---

## Notas Legales Importantes

- La plataforma provee información y facilita la gestión documental, pero **no es un agente aduanero**.
- Para procesos extra-UE, se recomienda siempre un **despachante de aduanas certificado**.
- Las tasas y normativas se actualizan periódicamente — siempre verificar con las autoridades competentes.
- La información contenida en `compliance.country_requirements` debe ser revisada por el equipo legal cada trimestre.
