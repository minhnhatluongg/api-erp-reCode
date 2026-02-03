namespace ERP_Portal_RC.Domain.Common
{
    /// <summary>
    /// Generic API Response wrapper cho tất cả API endpoints
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của data</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int StatusCode { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? Meta { get; set; }

        public ApiResponse()
        {
        }
        
        public ApiResponse(T data, string message = "Thành công")
        {
            Success = true;
            Data = data;
            Message = message;
            StatusCode = 200;
        }
        
        public static ApiResponse<T> SuccessResponse(T data, string message = "Thành công", int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow
            };
        }

        
        public static ApiResponse<T> ErrorResponse(string message, int statusCode = 400, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = statusCode,
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };
        }

      
        public static ApiResponse<T> SuccessResponseWithMeta(
            T data, 
            Dictionary<string, object> meta, 
            string message = "Thành công")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
                StatusCode = 200,
                Meta = meta,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    
    public class ApiResponse : ApiResponse<object>
    {
        public static new ApiResponse SuccessResponse(string message = "Thành công", int statusCode = 200)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow
            };
        }

        public static new ApiResponse ErrorResponse(string message, int statusCode = 400, List<string>? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                StatusCode = statusCode,
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };
        }
    }
       
    public class PagedResponse<T> : ApiResponse<List<T>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedResponse(List<T> data, int pageNumber, int pageSize, int totalRecords)
        {
            Success = true;
            Data = data;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalRecords = totalRecords;
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            Message = "Thành công";
            StatusCode = 200;
        }

        public static PagedResponse<T> Create(
            List<T> data, 
            int pageNumber, 
            int pageSize, 
            int totalRecords,
            string message = "Thành công")
        {
            return new PagedResponse<T>(data, pageNumber, pageSize, totalRecords)
            {
                Message = message
            };
        }
    }
}
