# RuleEngine API Example

This example demonstrates how to consume RuleEngineCLI as a REST API service using ASP.NET Core.

## Features

- RESTful API endpoints for rule validation
- Swagger/OpenAPI documentation
- JSON request/response handling
- Error handling and validation
- Configurable rule evaluation (enabled rules only or all rules)

## API Endpoints

### POST /api/rules/validate
Validates input data against business rules (enabled rules only).

**Request Body:**
```json
{
  "rulesFilePath": "path/to/rules.json",
  "inputJson": "{\"field1\": \"value1\", \"field2\": 123}"
}
```

**Response:**
```json
{
  "isValid": true,
  "violations": [],
  "executionTimeMs": 45,
  "rulesEvaluated": 5
}
```

### POST /api/rules/validate/advanced
Validates with additional configuration options.

**Request Body:**
```json
{
  "rulesFilePath": "path/to/rules.json",
  "inputJson": "{\"field1\": \"value1\", \"field2\": 123}",
  "onlyEnabledRules": true
}
```

**Response:** Same as above

## Running the Example

1. Navigate to the example directory:
   ```bash
   cd examples/RuleEngineApi
   ```

2. Run the API:
   ```bash
   dotnet run
   ```

3. Open Swagger UI at `http://localhost:5000/swagger`

4. Test the endpoints using the Swagger interface

## Example Usage

### Using PowerShell
```powershell
# Create test data
$body = @{
    rulesFilePath = "../../examples/rules.json"
    inputJson = '{"name":"John Doe","age":25,"email":"john@example.com"}'
} | ConvertTo-Json

# Make request
Invoke-WebRequest -Uri "http://localhost:5000/api/rules/validate" `
    -Method POST -Body $body -ContentType "application/json"
```

### Using curl
```bash
curl -X POST "http://localhost:5000/api/rules/validate" \
  -H "Content-Type: application/json" \
  -d '{
    "rulesFilePath": "../../examples/rules.json",
    "inputJson": "{\"name\": \"John Doe\", \"age\": 25, \"email\": \"john@example.com\"}"
  }'
```

### Using HTTPie
```bash
http POST http://localhost:5000/api/rules/validate \
  rulesFilePath="../../examples/rules.json" \
  inputJson='{"name":"John Doe","age":25,"email":"john@example.com"}'
```

## Integration Benefits

- **Scalability**: Handle multiple concurrent validation requests
- **Documentation**: Auto-generated API docs via Swagger
- **Testing**: Easy to test with HTTP clients
- **Monitoring**: Standard web API monitoring capabilities
- **Deployment**: Can be containerized and deployed anywhere

## Architecture

The API follows Clean Architecture principles:

- **Controllers**: Handle HTTP requests/responses
- **Application Services**: Business logic orchestration
- **Domain**: Core business rules and entities
- **Infrastructure**: External concerns (file I/O, evaluation)

## Configuration

The API uses dependency injection to configure RuleEngineCLI components:

- `IRuleEngine`: Main rule evaluation service
- `IRuleRepository`: Loads rules from JSON files
- `IExpressionEvaluator`: Evaluates rule expressions
- `ILogger`: Application logging

## Error Handling

The API returns appropriate HTTP status codes:
- `200 OK`: Successful validation
- `400 Bad Request`: Invalid input or rule file not found
- `500 Internal Server Error`: Unexpected errors

Error responses include detailed error messages for debugging.

1. Navigate to the example directory:
   ```bash
   cd examples/RuleEngineApi
   ```

2. Run the API:
   ```bash
   dotnet run
   ```

3. Open Swagger UI at `https://localhost:5001/swagger`

4. Test the endpoints using the Swagger interface or tools like Postman

## Example Usage

```bash
# Test with valid input
curl -X POST "https://localhost:5001/api/rules/validate" \
  -H "Content-Type: application/json" \
  -d '{
    "rulesFilePath": "../../examples/rules.json",
    "inputJson": "{\"name\": \"John Doe\", \"age\": 25, \"email\": \"john@example.com\"}"
  }'

# Test with invalid input
curl -X POST "https://localhost:5001/api/rules/validate" \
  -H "Content-Type: application/json" \
  -d '{
    "rulesFilePath": "../../examples/rules.json",
    "inputJson": "{\"name\": \"\", \"age\": 15, \"email\": \"invalid-email\"}"
  }'
```

## Integration Benefits

- **Scalability**: Handle multiple concurrent validation requests
- **Monitoring**: Built-in metrics and logging
- **Caching**: Optional performance optimization
- **Documentation**: Auto-generated API docs
- **Testing**: Easy to test with HTTP clients