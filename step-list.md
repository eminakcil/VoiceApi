# Development Steps

## Completed Features
- [x] Global exception handler (Implemented `GlobalExceptionHandler`)
- [x] Logging (Implemented `Serilog`)
- [x] Scalar - OpenApi (Implemented `Scalar.AspNetCore`)
- [x] AppDbContext (Implemented SQLite Context)
- [x] FluentValidation - ValidationFilter (Implemented `ValidationFilter<T>`)
- [x] Mapster (Implemented `MapsterConfig`)
- [x] Minimal API (Refactored to `AuthEndpoints`)
- [x] JWT (Implemented `JwtBearer` auth)
- [x] Role or Policy based authorization (Implemented `AdminOnly` policy)
- [x] Result pattern Result<T> (Implemented `ApiResponse<T>`)
- [x] Token Rotation (Implemented in `AuthService`)

## Planned Development Order

### Phase 5: Core Entity Refactoring
*Switching to robust Entity base with GuidV7 and Auditing*
- [ ] **Base Entity**: Create abstract `BaseEntity`
- [ ] **BaseEntity Id Guid7**: Switch ID strategy from `int` to `Guid.CreateVersion7`
- [ ] **Audit Interceptor**: Auto-fill `CreatedAt`, `UpdatedAt`, `DeletedAt`

### Phase 6: Soft Delete Strategy
*Implementing "Trash bin" logic*
- [ ] **Soft delete**: Add `ISoftDelete` interface and properties
- [ ] **SoftDelete Filter**: Apply Global Query Filter in DbContext

### Phase 7: Configuration Best Practices
*Moving from raw strings to strongly-typed options*
- [ ] **Config DTO**: Implement Options Pattern (e.g., `JwtSettings` class)
- [ ] **Config Validation**: Add validation for options on startup

### Phase 8: Observability (Aspire)
*Cloud-native monitoring*
- [ ] **Aspire Dashboard**: Add Aspire Orchestrator project and dashboard
