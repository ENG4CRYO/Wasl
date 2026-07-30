using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Common.Models;

namespace Wasl.Application.Features.Profile.Commands.UpdateRiderPhoto
{
    public class UpdateRiderPhotoCommand : IRequest<ApiResponse<string>>
    {
        public UploadedFile Photo { get; set; } = default!;
    }
}
