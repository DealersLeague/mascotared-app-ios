# Guía de Estilos MascotaRed

## Tipografía

### Titulares (Usar Inter Semibold o Bold)
- **Heading1**: `FontFamily="InterSemiBold"` o `FontFamily="InterBold"`, `FontSize="32"`
- **Heading2**: `FontFamily="InterSemiBold"` o `FontFamily="InterBold"`, `FontSize="24"`
- **Heading3**: `FontFamily="InterSemiBold"` o `FontFamily="InterBold"`, `FontSize="20"`
- **Heading4**: `FontFamily="InterSemiBold"` o `FontFamily="InterBold"`, `FontSize="18"`

**Ejemplo:**
```xml
<Label Text="Título Principal" FontFamily="InterSemiBold" FontSize="22" />
```

### Texto Base (Usar Inter Regular o OpenSans Regular)
- **BodyText**: `FontFamily="InterRegular"` o `FontFamily="OpenSansRegular"`, `FontSize="14"`
- **BodyTextSmall**: `FontFamily="InterRegular"` o `FontFamily="OpenSansRegular"`, `FontSize="12"`

**Ejemplo:**
```xml
<Label Text="Texto de contenido" FontFamily="InterRegular" FontSize="14" />
```

## Sistema UI

### Botón Principal
- **Fondo**: Azul #455AEB
- **Texto**: Blanco
- **CornerRadius**: 12px
- **Estilo**: `Style="{StaticResource Button}"` (por defecto)

**Ejemplo:**
```xml
<Button Text="Guardar" />
<!-- O explícitamente -->
<Button Text="Guardar" 
        BackgroundColor="#455AEB" 
        TextColor="White"
        CornerRadius="12" />
```

### Botón Secundario
- **Fondo**: Blanco
- **Borde**: Azul #455AEB (2px)
- **Texto**: Azul #455AEB
- **CornerRadius**: 12px
- **Estilo**: `Style="{StaticResource SecondaryButton}"`

**Ejemplo:**
```xml
<Button Text="Cancelar" Style="{StaticResource SecondaryButton}" />
```

### Botón Destacado
- **Fondo**: Rosa #FE3D7D
- **Texto**: Blanco
- **CornerRadius**: 12px
- **Estilo**: `Style="{StaticResource AccentButton}"`

**Ejemplo:**
```xml
<Button Text="Publicar" Style="{StaticResource AccentButton}" />
```

### Tarjetas
- **Fondo**: Blanco
- **Sombra**: Suave
- **Bordes redondeados**: 12-16px
- **Estilos disponibles**:
  - `CardStyleSmall`: CornerRadius="12"
  - `CardStyle`: CornerRadius="14" (recomendado)
  - `CardStyleLarge`: CornerRadius="16"

**Ejemplo:**
```xml
<Frame Style="{StaticResource CardStyle}">
    <!-- Contenido de la tarjeta -->
</Frame>
```

## Colores Corporativos

- **Azul Principal**: `#455AEB` - `{StaticResource Blue}` o `{StaticResource MascotaRedBlue}`
- **Rosa Acento**: `#FE3D7D` - `{StaticResource Pink}` o `{StaticResource MascotaRedPink}`
- **Gris Oscuro**: `#2C2C2C` - `{StaticResource Dark}` o `{StaticResource MascotaRedDark}`
- **Gris Claro**: `#F4F6FA` - `{StaticResource Light}` o `{StaticResource MascotaRedLight}`

## Buenas Prácticas

1. **Títulos de sección**: Usar `InterSemiBold` con tamaños 17-24px
2. **Títulos principales**: Usar `InterSemiBold` o `InterBold` con tamaños 20-32px
3. **Texto de contenido**: Usar `InterRegular` con tamaño 14px
4. **Texto pequeño**: Usar `InterRegular` con tamaño 12px
5. **Botones principales**: Usar el estilo por defecto (azul)
6. **Botones de acción destacada**: Usar `AccentButton` (rosa)
7. **Tarjetas**: Usar `CardStyle` para consistencia visual
