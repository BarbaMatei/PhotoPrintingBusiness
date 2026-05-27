namespace PhotoPrint.API.DTOs.Cart;

/// <summary>Body for POST /api/cart/merge — provides the guest session to merge into the authenticated user's cart.</summary>
public record CartMergeRequest(Guid GuestSessionId);
