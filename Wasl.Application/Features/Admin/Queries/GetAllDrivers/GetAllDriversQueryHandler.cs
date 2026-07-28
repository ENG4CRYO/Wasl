using MediatR;
using Microsoft.EntityFrameworkCore;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Admin;
using Wasl.Application.Interfaces.Common;

namespace Wasl.Application.Features.Admin.Queries.GetAllDrivers
{
    public class GetAllDriversQueryHandler : IRequestHandler<GetAllDriversQuery, ApiResponse<PagedList<DriverListDto>>>
    {
        private readonly IApplicationDbContext _context;

        public GetAllDriversQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedList<DriverListDto>>> Handle(GetAllDriversQuery request, CancellationToken cancellationToken)
        {
            var query = _context.DriverProfiles
                .Include(dp => dp.User)
                .AsNoTracking()
                .AsQueryable();

            if (request.StatusFilter.HasValue)
            {
                query = query.Where(dp => dp.ApprovalStatus == request.StatusFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                string search = request.SearchTerm.ToLower();
                query = query.Where(dp =>
                    dp.User.FirstName.ToLower().Contains(search) ||
                    dp.User.LastName.ToLower().Contains(search) ||
                    dp.User.PhoneNumber.Contains(search));
            }

            query = query.OrderByDescending(dp => dp.CreatedAt);

            var mappedQuery = query.Select(dp => new DriverListDto
            {
                DriverId = dp.UserId,
                FullName = dp.User.FirstName + " " + dp.User.LastName,
                PhoneNumber = dp.User.PhoneNumber!,
                Balance = dp.User.Balance,
                Status = dp.ApprovalStatus,
                SubmittedAt = dp.CreatedAt
            });

            var pagedDrivers = await PagedList<DriverListDto>.CreateAsync(
                mappedQuery,
                request.PageNumber,
                request.PageSize);

            return ApiResponse<PagedList<DriverListDto>>.Success(pagedDrivers, "تم جلب بيانات السائقين بنجاح.");
        }
    }
}