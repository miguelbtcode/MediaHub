namespace MediaHub.Validation
{
    /// <summary>
    /// Validation failure
    /// </summary>
    public class ValidationFailure
    {
        /// <summary>
        /// Property name
        /// </summary>
        public string PropertyName { get; }
        
        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Initializes a new instance of the ValidationFailure class
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="errorMessage">Error message</param>
        public ValidationFailure(string propertyName, string errorMessage)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }
    }
}