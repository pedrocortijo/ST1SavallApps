# Plan de integración Wialon: seguimiento en Mis servicios

## Objetivo

Mostrar en cada servicio asignado la ubicación real del camión, su estado de conexión y una estimación del tiempo restante hasta la obra.

## Alcance previsto

- Posición GPS actual del camión.
- Velocidad, rumbo y hora del último mensaje.
- Kilómetros y tiempo estimado hasta la obra.
- Estados operativos: en ruta, parado, sin señal, posición antigua y sin camión asignado.
- Acceso al detalle de seguimiento desde `Mis servicios`.

## Plan de actuación

1. Validar Wialon
   - Confirmar si se utiliza Wialon Hosting o Wialon Local.
   - Registrar la URL/centro de datos de la instancia.
   - Crear un token técnico de solo lectura con acceso a unidades y seguimiento.
   - Identificar las unidades disponibles y comprobar sus posiciones reales.

2. Completar asociaciones internas
   - Verificar la cadena `Conductor -> Camión -> UnidadWialonId`.
   - Mantener la unicidad de la unidad Wialon por camión.
   - Validar coordenadas de plantas y obras para cada solicitud.

3. Integrar Wialon en la API
   - Crear un cliente Wialon del lado servidor.
   - Gestionar autenticación, sesión y renovación internamente.
   - No exponer el token de Wialon al navegador.
   - Normalizar los datos en un DTO propio: posición, velocidad, rumbo, fecha GPS y conectividad.

4. Exponer seguimiento por servicio
   - Añadir `GET /api/servicios/{id}/seguimiento`.
   - Resolver el conductor, camión y unidad Wialon del servicio.
   - Devolver la última posición junto con la antigüedad del dato.

5. Calcular ETA
   - Origen: última posición GPS del camión.
   - Destino: coordenadas de la obra.
   - Primera fase: estimación por distancia y velocidad actual/media.
   - Segunda fase: usar ruta planificada de Wialon Logistics, si está contratado y configurado.
   - Definir resultados explícitos si no hay posición reciente, ruta o camión asignado.

6. Integrar la interfaz
   - Añadir a cada tarjeta de `Mis servicios` estado del camión, hora GPS, distancia y ETA.
   - Incorporar un botón `Ver seguimiento`.
   - Actualizar manualmente y de forma automática cada 30-60 segundos, con límites para evitar consultas innecesarias.

7. Validar y desplegar
   - Realizar una prueba inicial con uno o dos camiones.
   - Comparar la posición mostrada con Wialon.
   - Probar pérdida de señal, posición antigua, vehículo parado y llegada a obra.
   - Activar la funcionalidad progresivamente para toda la flota.

## Datos necesarios antes de implementar

- Tipo de Wialon y URL de acceso.
- Token técnico de lectura.
- Confirmación de las unidades que corresponden a cada camión.
- Confirmación de si Wialon Logistics está contratado para calcular rutas planificadas.
