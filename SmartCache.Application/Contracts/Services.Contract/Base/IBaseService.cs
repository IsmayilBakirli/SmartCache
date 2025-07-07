namespace SmartCache.Application.Contracts.Services.Contract.Base
{
    public interface IBaseService<TGetDto, TCreateDto, TUpdateDto>
    {
        Task<List<TGetDto>> GetAllAsync(int skip = 0, int take = int.MaxValue);

        Task<TGetDto> GetByIdAsync(int id);

        Task CreateAsync(TCreateDto createDto);

        Task UpdateAsync(TUpdateDto updateDto);

        Task DeleteAsync(int id);
    }
}
