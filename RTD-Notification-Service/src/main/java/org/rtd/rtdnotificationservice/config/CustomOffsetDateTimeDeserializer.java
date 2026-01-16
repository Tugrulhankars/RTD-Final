package org.rtd.rtdnotificationservice.config;

import com.fasterxml.jackson.core.JsonParser;
import com.fasterxml.jackson.databind.DeserializationContext;
import com.fasterxml.jackson.databind.JsonDeserializer;
import java.io.IOException;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeFormatterBuilder;
import java.time.temporal.ChronoField;

public class CustomOffsetDateTimeDeserializer extends JsonDeserializer<OffsetDateTime> {
    
    // C# DateTime formatı: yyyy-MM-ddTHH:mm:ss[.SSSSSSS] (timezone offset yok)
    private static final DateTimeFormatter LOCAL_DATETIME_FORMATTER = new DateTimeFormatterBuilder()
            .appendPattern("yyyy-MM-dd'T'HH:mm:ss")
            .appendFraction(ChronoField.NANO_OF_SECOND, 0, 9, true) // 0-9 haneli nanosecond
            .toFormatter();
    
    @Override
    public OffsetDateTime deserialize(JsonParser p, DeserializationContext ctxt) throws IOException {
        String dateString = p.getText();
        if (dateString == null || dateString.isEmpty()) {
            return null;
        }
        
        String cleaned = dateString.trim();
        
        try {
            // Eğer zaten timezone offset varsa (Z, +HH:mm, -HH:mm), direkt parse et
            if (cleaned.endsWith("Z") || cleaned.matches(".*[+-]\\d{2}:?\\d{2}$")) {
                return OffsetDateTime.parse(cleaned);
            }
            
            // C# tarafından gelen format: yyyy-MM-ddTHH:mm:ss[.SSSSSSS] (timezone offset yok)
            // LocalDateTime olarak parse et ve UTC olarak kabul et
            LocalDateTime localDateTime = LocalDateTime.parse(cleaned, LOCAL_DATETIME_FORMATTER);
            return localDateTime.atOffset(ZoneOffset.UTC);
            
        } catch (Exception e) {
            // Fallback: ISO formatını dene
            try {
                return OffsetDateTime.parse(cleaned);
            } catch (Exception e2) {
                throw new IOException("Cannot deserialize OffsetDateTime from: " + dateString + ". Error: " + e.getMessage(), e2);
            }
        }
    }
}

