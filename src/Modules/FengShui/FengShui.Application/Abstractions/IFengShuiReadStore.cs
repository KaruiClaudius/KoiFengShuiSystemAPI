using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Abstractions
{
    public interface IFengShuiReadStore
    {
        Task<Element?> GetElementByNameAsync(string elementName);
        Task<IReadOnlyList<Element>> GetAllElementsAsync();
        Task<Direction?> GetDirectionByNameAsync(string directionName);
        Task<FengShuiDirection?> GetFengShuiDirectionAsync(int directionId, int elementId);
        Task<ShapeCategory?> GetShapeByNameAndElementIdAsync(string shapeName, int elementId);
        Task<IReadOnlyList<ShapeCategory>> GetAllShapeCategoriesAsync();
        Task<IReadOnlyList<KoiBreed>> GetAllKoiBreedsAsync();
        Task<IReadOnlyList<FengShuiDirection>> GetAllFengShuiDirectionsWithDirectionAsync();
        Task<Element?> GetElementByIdAsync(int elementId);
        Task<IReadOnlyList<FengShuiDirection>> GetFengShuiDirectionsByElementIdAsync(int elementId);
        Task<IReadOnlyList<Direction>> GetAllDirectionsAsync();
    }
}
