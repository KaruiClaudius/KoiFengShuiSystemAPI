using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.Common;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class MarketplaceListingService : IMarketplaceListingService
    {
        private readonly UnitOfWorkRepository _unitOfWork;

        public MarketplaceListingService()
        {
            _unitOfWork = new UnitOfWorkRepository();
        }

        public async Task<IBusinessResult> CreateMarketplaceListing(MarketplaceListing marketplaceListing)
        {
            try
            {
                var entityInDb = await _unitOfWork.MarketplaceListingRepository.GetByIdAsync(marketplaceListing.ListingId);

                // If Post already exists, return an error indicating duplicate Post
                if (entityInDb != null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, "Post already exists. Cannot create a new Post with the same ID.");
                }

                // If Post doesn't exist, create a new one
                await _unitOfWork.MarketplaceListingRepository.CreateAsync(marketplaceListing);
                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG);
            }
            catch (Exception ex)
            {
                return new BusinessResult(-4, ex.Message);
            }
        }

        public async Task<IBusinessResult> DeleteMarketplaceListing(int id)
        {
            try
            {
                var marketplaceListing = await _unitOfWork.MarketplaceListingRepository.GetByIdAsync(id);
                if (marketplaceListing != null)
                {
                    var result = await _unitOfWork.MarketplaceListingRepository.RemoveAsync(marketplaceListing);
                    if (result)
                    {
                        return new BusinessResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
                    }
                }
                else
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA__MSG);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(-4, ex.ToString());
            }
        }

        public async Task<IBusinessResult> GetAll()
        {
            try
            {
                var marketplaceListings = await _unitOfWork.MarketplaceListingRepository.GetAllWithElementAsync();
                if (marketplaceListings != null)
                {
                    var marketplaceListingsResponses = marketplaceListings.Select(mp => new MarketplaceListingResponse
                    {
                        ListingId = mp.ListingId,
                        AccountId = mp.AccountId,
                        TierId = mp.TierId,
                        Title = mp.Title,
                        Description = mp.Description,
                        Price = mp.Price,
                        Quantity = mp.Quantity,
                        CategoryId = mp.CategoryId,
                        CreateAt = DateTime.Now,
                        ExpiresAt = mp.ExpiresAt,
                        IsActive = mp.IsActive,
                        ElementId = mp.ElementId,
                        AccountName = mp.Account.FullName,
                        ElementName = mp.Element.ElementName,
                        TierName = mp.Tier.TierName,
                        Status = mp.Status,
                    }).ToList();
                    if (marketplaceListingsResponses == null)
                    {
                        return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, marketplaceListingsResponses);
                    }
                }
                return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);

            }
            catch (Exception e)
            {
                return new BusinessResult(-4, e.Message.ToString());
            }
        }

        public async Task<IBusinessResult> GetMarketplaceListingByCategoryId(int categoryId, int pageNumber, int pageSize)
        {
            try
            {
                var marketplaceListings = await _unitOfWork.MarketplaceListingRepository.GetAllByPostTypeIdAsync(categoryId, pageNumber, pageSize);
                if (marketplaceListings != null)
                {
                    var marketplaceListingsResponses = marketplaceListings.Select(mp => new MarketplaceListingResponse
                    {
                        ListingId = mp.ListingId,
                        AccountId = mp.AccountId,
                        TierId = mp.TierId,
                        Title = mp.Title,
                        Description = mp.Description,
                        Price = mp.Price,
                        Quantity = mp.Quantity,
                        CategoryId = mp.CategoryId,
                        CreateAt = DateTime.Now,
                        ExpiresAt = mp.ExpiresAt,
                        IsActive = mp.IsActive,
                        ElementId = mp.ElementId,
                        AccountName = mp.Account.FullName,
                        ElementName = mp.Element.ElementName,
                        TierName = mp.Tier.TierName,
                        Status = mp.Status,
                    }).ToList();
                    if (marketplaceListingsResponses == null)
                    {
                        return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, marketplaceListingsResponses);
                    }
                }
                return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);

            }
            catch (Exception e)
            {
                return new BusinessResult(-4, e.Message.ToString());
            }
        }

        public async Task<IBusinessResult> GetMarketplaceListingById(int id)
        {
            try
            {
                var Post = await _unitOfWork.MarketplaceListingRepository.GetByIdAsync(id);
                if (Post == null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                }
                else
                {
                    return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, Post);
                }
            }
            catch (Exception e)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, e.Message);
            }
        }

        public async Task<IBusinessResult> Save()
        {
            try
            {
                var result = await _unitOfWork.MarketplaceListingRepository.SaveAsync();
                if (result > 0)
                {
                    return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG);
                }
                else
                {
                    return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
                }
            }
            catch (Exception e)
            {
                return new BusinessResult(-4, e.Message.ToString());
            }
        }
    }
}
