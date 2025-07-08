namespace SmartCache.Application.Contracts.Services.Contract.Base
{
    public interface IBaseService<TGetDto, TCreateDto, TUpdateDto>
    {
        Task<List<TGetDto>> GetAllAsync();

        Task<TGetDto> GetByIdAsync(int id);

        Task<int> GetVersionAsync();

        Task<bool> CheckVersionChange(int clientId);

        Task CreateAsync(TCreateDto createDto);

        Task UpdateAsync(TUpdateDto updateDto);

        Task DeleteAsync(int id);
    }
}
