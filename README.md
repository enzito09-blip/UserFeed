# UserFeed Microservice

Microservicio de comentarios de usuarios sobre artículos del catálogo, implementado con .NET 6 y Arquitectura Hexagonal.

## Tecnologías

- **.NET 6** - Framework
- **MongoDB** - Base de datos
- **RabbitMQ** - Mensajería asíncrona
- **Swagger** - Documentación de API
- **Arquitectura Hexagonal** - Patrón de diseño

## Arquitectura

El proyecto sigue la Arquitectura Hexagonal (Puertos y Adaptadores):

```
UserFeed.Domain         - Núcleo del negocio (Entities, Ports)
UserFeed.Application    - Casos de uso (Use Cases, DTOs)
UserFeed.Infrastructure - Adaptadores (MongoDB, RabbitMQ, Auth)
UserFeed.Api            - Capa de presentación (Controllers, Config)
```

## Requisitos

- .NET 6 SDK
- Docker (para MongoDB y RabbitMQ)

## Instalación

### 1. Levantar contenedores de infraestructura

```bash
docker run -d --name ec-mongo -p 27017:27017 mongo:6.0
docker run -d --name ec-rabbitmq -p 15672:15672 -p 5672:5672 rabbitmq:3.13.6-management
```

### 2. Ejecutar el microservicio

```bash
cd userfeed-service
dotnet run --project UserFeed.Api
```

La API estará disponible en: http://localhost:5005

## Swagger

La documentación interactiva de la API está disponible en:

http://localhost:5005

## Endpoints

### POST /api/v1/comments
Crear un comentario (requiere autenticación)

### GET /api/v1/comments/article/{articleId}
Obtener comentarios de un artículo (público)

### PUT /api/v1/comments/{id}
Actualizar un comentario (solo el autor)

### DELETE /api/v1/comments/{id}
Eliminar un comentario (solo el autor)

## Autenticación

Todos los endpoints (excepto GET) requieren un token JWT del servicio Auth.

Header:
```
Authorization: Bearer {token}
```

## Eventos RabbitMQ

El microservicio escucha eventos de logout del servicio Auth para invalidar tokens del cache.

Exchange: `auth` (fanout)

## Variables de Entorno

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "userfeed_db",
    "CollectionName": "comments"
  },
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  },
  "AuthService": {
    "BaseUrl": "http://localhost:3000"
  }
}
```

## Docker

### Construir imagen

```bash
docker build -t userfeed-service .
```

### Ejecutar contenedor (Windows/Mac)

```bash
docker run -d --name userfeed-service -p 5005:5005 userfeed-service
```

### Ejecutar contenedor (Linux)

```bash
docker run --add-host host.docker.internal:172.17.0.1 -d --name userfeed-service -p 5005:5005 userfeed-service
```

## Tests

El proyecto incluye tests unitarios y de integración exhaustivos (52 tests) que cubren todos los endpoints y casos de uso.

### Ejecutar tests

```bash
dotnet test
```

### Ejecutar tests con salida detallada

```bash
dotnet test --verbosity detailed
```

### Cobertura de tests

Los tests cubren:
- ✅ **POST /api/v1/comments**: Creación exitosa, validaciones de rating (1-5), longitud de comentario (≤500), artículo inexistente, usuario sin compra, duplicados, token faltante, claims alternativos (userId/userID/uid/sub)
- ✅ **GET /api/v1/comments/article/{articleId}**: Paginación, pageSize válidos (10,20,50,80,100), exclusión de comentarios eliminados, validaciones de página
- ✅ **GET /api/v1/comments/my-comments**: Comentarios del usuario autenticado, token faltante, exclusión de eliminados
- ✅ **PUT /api/v1/comments/{id}**: Actualización exitosa, validaciones de input, comentario eliminado, no encontrado, sin autorización
- ✅ **DELETE /api/v1/comments/{id}**: Eliminación lógica exitosa, ya eliminado, no encontrado, sin autorización

**Resultado**: 52/52 tests pasados ✅ (sin bugs detectados)

## Licencia

MIT
