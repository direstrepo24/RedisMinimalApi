# Redis

Example manage other data types:

Datos para prueba y keys:

1- myList: ["item1", "item2", "item3"]
2- mySet:  ["newUnique1", "newUnique2"]
3- mySortedSet:{"newMember1": 4.0, "newMember2": 5.0}

# Para Probar cada API REST del ejemplo de clientes:
Cargar datos: ejecutando una solicitud POST a /loadClients.
Paginación: Accede a los clientes de manera paginada utilizando GET /clients?page=1&pageSize=100


## Configuración Inicial de Redis:
Asegúrate de tener Redis instalado y ejecutándose en tu máquina o en un entorno accesible.

## Ejecución del Proyecto:
Ejecuta tu proyecto ASP.NET Core para asegurarte de que los endpoints están disponibles. Puedes usar dotnet run para iniciar la aplicación.

## Endpoints Implementados:
- Utiliza Postman, cURL, o Swagger UI para enviar una solicitud POST a /loadClients.

- curl -X POST http://localhost:5278/loadClients
Esta operación cargará un millón de clientes en Redis en lotes de 10,000.
Prueba de Paginación de Clientes:

Utiliza Postman, cURL, o Swagger UI para enviar una solicitud GET a /clients con los parámetros de paginación.

- curl "http://localhost:5278/clients?page=1&pageSize=10000"

Cambia los parámetros page y pageSize para probar diferentes rangos de paginación.

Batching: Las operaciones HashSetAsync y ListRightPushAsync se agregan a un batch. batch.Execute() no es necesario ya que las operaciones se disparan al finalizar la espera de todas las tareas con Task.WhenAll(tasks).
Task Management: Task.WhenAll(tasks) espera a que todas las tareas en el lote se completen. Esto asegura que todas las operaciones de Redis en el batch sean enviadas y completadas antes de que el método retorne.
Eficiencia: Usar batching en este modo permite agrupar múltiples operaciones en una sola llamada a Redis, lo cual puede mejorar significativamente la eficiencia cuando se manejan grandes volúmenes de datos.


# Explicación de los tipos de estructura en Redis
1. Listas ([]):
    * En Redis, las listas son estructuras de datos simples que almacenan una secuencia de strings en un orden específico.
    * Las listas se representan como arrays ([]) porque preservan el orden de inserción y permiten duplicados.
    * Operaciones típicas incluyen: LPUSH, RPUSH para agregar elementos al principio o al final de la lista, LPOP, RPOP para remover elementos, y LRANGE para obtener un rango de elementos.
    * Las listas son ideales para colas de trabajos, historiales de actividades, o simplemente como una colección ordenada de elementos.
2. Conjuntos ({}):
    * Los conjuntos en Redis son colecciones de strings únicos que no preservan ningún orden.
    * Se representan con llaves ({}) en notaciones JSON para subrayar que son colecciones de elementos únicos sin un orden inherente.
    * Operaciones comunes sobre conjuntos incluyen: SADD para agregar elementos, SREM para remover elementos, y SMEMBERS para obtener todos los miembros del conjunto.
    * Los conjuntos son útiles para almacenar elementos únicos como tags, votos, o cualquier colección donde la unicidad es más importante que el orden.
3. Conjuntos Ordenados ({}):
    * Los conjuntos ordenados combinan características de ambos: almacenan elementos únicos, cada uno asociado con una puntuación que determina el orden.
    * También se representan con llaves ({}) en formatos JSON, pero cada elemento es un par clave-valor, donde la clave es el elemento y el valor es su puntuación.
    * Operaciones incluyen: ZADD para agregar o actualizar elementos, ZRANGE para obtener elementos ordenados por su puntuación, y ZREM para remover elementos.
    * Son perfectos para clasificaciones, índices de búsqueda ponderados, y sistemas de priorización.


# Mejores Prácticas y Estructuras de Datos en Redis
1. Uso de Hashes en Redis
* Ventajas: Los hashes son ideales para almacenar objetos con múltiples campos, como los registros de clientes con ID, nombre y otros posibles atributos. Usar un hash para cada cliente minimiza el espacio utilizado en comparación con tener múltiples claves de strings separadas para cada campo.
* Uso: Puedes almacenar cada cliente en un hash individual con client_id como clave principal, y los detalles del cliente como campos dentro del hash. Esto te permite recuperar, actualizar, o eliminar fácilmente un cliente específico con un solo ID.
2. Cuándo Usar Listas
* Las listas son mejores cuando necesitas representar relaciones ordenadas o cuando el orden de los elementos es importante, como en colas de mensajes o registros de actividades. Para una base de datos de clientes que necesita acceso rápido a registros individuales, las listas no son la opción óptima.
3. Uso de Conjuntos y Conjuntos Ordenados
* Los conjuntos son útiles para almacenar IDs de clientes cuando solo necesitas manejar la membresía (saber si un cliente existe o no) y garantizar la unicidad.
* Los conjuntos ordenados son útiles si necesitas mantener a los clientes ordenados por algún criterio, como la fecha de último contacto o un ranking de actividad.