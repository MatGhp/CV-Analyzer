using CVAnalyzer.AgentService.Models;
using CVAnalyzer.Domain.Entities;

namespace CVAnalyzer.Infrastructure.Mappers;

public static class CandidateInfoMapper
{
    public static CandidateInfo MapFromDto(CandidateInfoDto dto, Guid resumeId)
    {
        var skills = ParseSkills(dto.Skills);
        
        return new CandidateInfo
        {
            ResumeId = resumeId,
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            Location = dto.Location,
            Skills = skills,
            YearsOfExperience = dto.YearsOfExperience,
            CurrentJobTitle = dto.CurrentJobTitle,
            Education = dto.Education
        };
    }

    public static void UpdateFromDto(CandidateInfo entity, CandidateInfoDto dto)
    {
        var skills = ParseSkills(dto.Skills);
        
        entity.FullName = dto.FullName;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.Location = dto.Location;
        entity.Skills = skills;
        entity.YearsOfExperience = dto.YearsOfExperience;
        entity.CurrentJobTitle = dto.CurrentJobTitle;
        entity.Education = dto.Education;
    }

    private static List<string> ParseSkills(string skillsString)
    {
        return skillsString
            .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
