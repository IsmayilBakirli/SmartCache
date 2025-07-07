using SmartCache.Application.DTOs.Story;
using SmartCache.Domain.Entities;

namespace SmartCache.Application.MappingProfile
{
    public static class StoryMapping
    {
        public static StoryGetDto MapToStoryGetDto(this Story story)
        {
            return new StoryGetDto
            {
                Id = story.Id,
                Title = story.Title,
                Content = story.Content,
                ImageUrl = story.ImageUrl,
                IsPublished = story.IsPublished,
                CreatedDate = story.CreatedDate,
                UpdatedDate = story.UpdatedDate
            };
        }

        public static List<StoryGetDto> MapToStoryGetDtos(this List<Story> stories)
        {
            return stories.Select(story => story.MapToStoryGetDto()).ToList();
        }

        public static Story MapToStory(this StoryCreateDto createDto)
        {
            return new Story
            {
                Title = createDto.Title,
                Content = createDto.Content,
                ImageUrl = createDto.ImageUrl,
                IsPublished = createDto.IsPublished
            };
        }

        public static void MapToStory(this StoryUpdateDto updateDto, Story story)
        {
            story.Title = updateDto.Title;
            story.Content = updateDto.Content;
            story.ImageUrl = updateDto.ImageUrl;
            story.IsPublished = updateDto.IsPublished;
        }
        public static Story MapToStory(this StoryGetDto dto)
        {
            return new Story
            {
                Id = dto.Id,
                Title = dto.Title,
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                IsPublished = dto.IsPublished,
                CreatedDate = dto.CreatedDate,
                UpdatedDate = dto.UpdatedDate
            };
        }
    }
}
