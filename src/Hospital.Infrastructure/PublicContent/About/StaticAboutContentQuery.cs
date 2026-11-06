using Hospital.Application.Features.About;

namespace Hospital.Infrastructure.PublicContent.About;

public sealed class StaticAboutContentQuery : IAboutContentQuery
{
    public Task<AboutContent> GetAsync(CancellationToken cancellationToken = default)
    {
        var content = AboutContentSanitizer.Create(
            "About Meridian Hospital",
            "Advanced medicine, delivered with humanity",
            "For more than three decades, Meridian Hospital has brought specialist expertise, modern clinical facilities, and compassionate support together under one roof. Every care journey is shaped around the person, not only the condition.",
            new AboutImage(
                "https://images.unsplash.com/photo-1586773860418-d37222d8fce3?auto=format&fit=crop&w=1200&q=85",
                "https://images.unsplash.com/photo-1586773860418-d37222d8fce3?auto=format&fit=crop&w=720&q=80 720w, https://images.unsplash.com/photo-1586773860418-d37222d8fce3?auto=format&fit=crop&w=1200&q=85 1200w, https://images.unsplash.com/photo-1586773860418-d37222d8fce3?auto=format&fit=crop&w=1800&q=85 1800w",
                "The bright, welcoming entrance of a modern hospital",
                1200,
                800),
            new AboutStatistic?[]
            {
                new("30+", "Years of trusted care"),
                new("24/7", "Emergency and critical care"),
                new("20+", "Specialist departments"),
                new("100K+", "Patients supported")
            },
            "Quality you can feel at every step",
            new string?[]
            {
                "Our multidisciplinary teams combine evidence-based practice with thoughtful communication, so patients and families can understand their choices and take part in every decision.",
                "From infection prevention and medication safety to coordinated discharge planning, our standards are designed around reliable outcomes, dignity, and continuity of care."
            },
            new AboutImage(
                "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?auto=format&fit=crop&w=1200&q=85",
                "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?auto=format&fit=crop&w=720&q=80 720w, https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?auto=format&fit=crop&w=1200&q=85 1200w",
                "A hospital clinician speaking attentively with a patient",
                1200,
                800),
            new AboutHighlight?[]
            {
                new("Patient-first decisions", "Clear explanations, shared decisions, and respect for every person's needs."),
                new("Coordinated expertise", "Specialists, nurses, diagnostics, and support teams work as one care team."),
                new("Safer care systems", "Continuous review and evidence-led protocols support dependable quality.")
            });

        return Task.FromResult(content);
    }
}
