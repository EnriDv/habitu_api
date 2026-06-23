using System;
using System.Collections.Generic;

namespace Habitu.Application.DTOs;

public static class GoalTemplateCatalog
{
    public static readonly IReadOnlyList<TemplateGoalDto> Goals =
    [
        new("sleep_better", "Dormir mejor", "Rutinas pequeñas para descansar mejor y sostener tu energía."),
        new("exercise", "Hacer ejercicio", "Movimiento breve y repetible para construir consistencia."),
        new("focus", "Mejorar enfoque", "Hábitos para planificar, reducir fricción y ejecutar."),
        new("reading", "Leer o estudiar", "Aprendizaje sostenido con bloques pequeños."),
        new("save_money", "Ahorrar", "Hábitos de control y decisiones financieras conscientes."),
        new("home_order", "Ordenar el hogar", "Pequeñas acciones diarias para mantener tu espacio funcional."),
        new("mental_wellbeing", "Bienestar mental", "Pausas, respiración y hábitos para bajar el ruido mental."),
        new("relationships", "Cuidar relaciones", "Gestos cortos y frecuentes para fortalecer vínculos."),
    ];

    public static readonly IReadOnlyList<HabitTemplateDto> Templates =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Dormir antes de las 23:00", "Acorta tu hora de dormir para sostener descanso.", "sleep_better", "sleep", new List<string> { "night" }, "daily", new List<int> { 1, 2, 3, 4, 5, 6, 7 }, "#7C9EFF", "bedtime", true),
        new(Guid.Parse("11111111-1111-1111-1111-111111111112"), "No usar pantallas 30 min antes", "Reduce estimulación antes de dormir.", "sleep_better", "sleep", new List<string> { "night" }, "daily", new List<int> { 1, 2, 3, 4, 5, 6, 7 }, "#5C7CFA", "moon", true),
        new(Guid.Parse("22222222-2222-2222-2222-222222222221"), "Caminar 20 minutos", "Bloque corto de movimiento diario.", "exercise", "health", new List<string> { "morning", "outdoor" }, "daily", new List<int> { 1, 2, 3, 4, 5, 6, 7 }, "#7BD389", "walk", true),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Estirar 10 minutos", "Movilidad simple para mantener continuidad.", "exercise", "health", new List<string> { "morning", "home" }, "daily", new List<int> { 1, 2, 3, 4, 5, 6, 7 }, "#59C3C3", "stretch", false),
        new(Guid.Parse("33333333-3333-3333-3333-333333333331"), "Planificar el día", "Define 1 a 3 prioridades antes de empezar.", "focus", "productivity", new List<string> { "morning", "work" }, "daily", new List<int> { 1, 2, 3, 4, 5 }, "#FFC857", "plan", true),
        new(Guid.Parse("33333333-3333-3333-3333-333333333332"), "Bloque de enfoque 25 minutos", "Haz una sesión concreta sin interrupciones.", "focus", "productivity", new List<string> { "work" }, "daily", new List<int> { 1, 2, 3, 4, 5 }, "#FF9F1C", "focus", true),
        new(Guid.Parse("44444444-4444-4444-4444-444444444441"), "Leer 10 páginas", "Progreso pequeño pero constante.", "reading", "learning", new List<string> { "night", "study" }, "daily", new List<int> { 1, 2, 3, 4, 5, 6, 7 }, "#9B5DE5", "book", true),
        new(Guid.Parse("55555555-5555-5555-5555-555555555551"), "Registrar gasto del día", "Captura un gasto para mantener visibilidad.", "save_money", "finance", new List<string> { "finance" }, "daily", new List<int> { 1, 2, 3, 4, 5, 6, 7 }, "#2EC4B6", "wallet", true),
        new(Guid.Parse("66666666-6666-6666-6666-666666666661"), "Ordenar 10 minutos", "Mantén orden con una acción breve.", "home_order", "home", new List<string> { "home" }, "daily", new List<int> { 1, 2, 3, 4, 5, 6, 7 }, "#5FA8D3", "home", true),
        new(Guid.Parse("77777777-7777-7777-7777-777777777771"), "Respirar 5 minutos", "Pausa simple para regularte durante el día.", "mental_wellbeing", "wellbeing", new List<string> { "morning", "afternoon" }, "daily", new List<int> { 1, 2, 3, 4, 5, 6, 7 }, "#CDB4DB", "breathe", true),
        new(Guid.Parse("88888888-8888-8888-8888-888888888881"), "Escribir a alguien importante", "Refuerza un vínculo con una acción pequeña.", "relationships", "relationships", new List<string> { "social" }, "daily", new List<int> { 1, 3, 5 }, "#F28482", "message", true),
    ];
}
