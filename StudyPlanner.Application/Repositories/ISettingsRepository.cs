using StudyPlanner.Core.Entities;

namespace StudyPlanner.Application.Repositories
{
    public interface ISettingsRepository
    {
        /// <summary>
        /// Retorna as configurações atuais. Se não existirem, cria e retorna os padrões.
        /// </summary>
        Task<AppSettings> GetOrCreateAsync(CancellationToken ct = default);

        /// <summary>
        /// Atualiza as configurações no banco de dados.
        /// </summary>
        /// <param name="settings">As configurações a serem atualizadas.</param>
        /// <param name="ct">Token de cancelamento.</param> 
        Task UpdateAsync(AppSettings settings, CancellationToken ct = default);
    }
}
