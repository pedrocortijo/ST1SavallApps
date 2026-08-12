# Plan: albarán de venta Sage al finalizar un servicio

## Decisiones funcionales confirmadas

- El conductor finaliza el servicio desde la tableta. La API central realiza la creación del albarán, nunca la aplicación de la tableta directamente contra Sage.
- Se añade a `Parametros` la configuración por empresa:
  - `SerieAlbaranVentaSage` (máximo 2 caracteres): serie de venta, por ejemplo `A`.
  - `AlmacenAlbaranVentaSage` (máximo 3 caracteres): almacén por defecto.
  - `UsuarioAlbaranVentaSage` (máximo 25 caracteres): valor de `c_albven.USUARIO` y `d_albven.USUARIO`.
- La fecha del albarán será la fecha y hora real de finalización del servicio.
- `Solicitud.IdCliente` se mantiene como entero porque es la referencia interna a `Obras.IdObra`; no se convierte a texto ni se reutiliza como código Sage.
- El cliente Sage será siempre el **Cliente de Servicio** de la obra: `obras.CLIENTE` / `ObraComunSage50.Cliente`, que ya es `char(8)`. Esta regla evita ambigüedades cuando una obra o un cliente dispone de más de un código en Sage.
- Cada línea corresponde al código de artículo guardado en `Solicitud.TipoResiduo`; se obtiene su descripción y demás datos necesarios de `articulo` en Sage.
- Todos los servicios, independientemente del tipo de tarea, generarán un albarán con una única línea: una unidad y precio inicial cero. Por ello `TipoResiduo` estará disponible y será obligatorio para todos los servicios.

## Desarrollo

1. Configuración
   - Añadir los tres campos al modelo, API, migración y pantalla de Parámetros.
   - Validar que serie, almacén y usuario estén configurados antes de finalizar.

2. Servicio transaccional de albaranes
   - Crear `AlbaranVentaSageService` en la API.
   - Resolver la obra desde `Solicitud.IdCliente`, obtener su Cliente de Servicio (`ObraComunSage50.Cliente`) y validar que exista en Sage.
   - Leer y bloquear de forma transaccional el contador de `series` para `TIPODOC = 4` (albarán de venta), empresa y serie configurada.
   - Insertar cabecera en `c_albven` y línea o líneas en `d_albven` con los valores de la solicitud y el artículo de residuo.
   - Actualizar el contador de la serie dentro de la misma transacción de Sage.

3. Finalización segura
   - Antes de pasar el servicio a Finalizado, validar firma, DNI, albarán de planta, contenedores obligatorios y, cuando sea retirada/cambio, tipo de residuo.
   - Crear el albarán Sage primero. Solo si la operación completa tiene éxito se marcará el servicio como Finalizado.
   - Guardar en la solicitud `AlbaranSerieSage` y `AlbaranNumeroSage` para impedir duplicados y ofrecer trazabilidad.
   - Si falla Sage o se pierde la conexión, el servicio permanecerá sin finalizar y la API devolverá un error recuperable; el conductor podrá reintentar desde la tableta sin que se cree un segundo albarán.

4. Pruebas
   - Prueba con entrega, retirada y cambio.
   - Prueba de reintento tras error de red y de concurrencia de dos finalizaciones.
   - Verificación de cabecera, línea, contador de serie y referencia guardada en el servicio.

## Pendiente de confirmar antes de activar producción

- Valores concretos que deban heredarse del cliente o artículo para la cabecera y línea de Sage (forma de pago, IVA, cuenta, familia, vendedor, etc.).
