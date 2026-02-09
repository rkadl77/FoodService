@echo off
echo Запуск тестов для Cart-service багов...
echo.

echo 1. Тестируем BuggyFeaturesService...
dotnet test --filter "FullyQualifiedName~BuggyFeaturesServiceTests"

echo.
echo 2. Тестируем БАГ 3 (количество не меняется при добавлении)...
dotnet test --filter "FullyQualifiedName~CartServiceBug3Tests"

echo.
echo 3. Тестируем БАГ 4 (количество не меняется при удалении)...
dotnet test --filter "FullyQualifiedName~CartServiceBug4Tests"

echo.
echo 4. Тестируем БАГ 5 (корзина не очищается)...
dotnet test --filter "FullyQualifiedName~CartServiceBug5Tests"

echo.
echo 5. Тестируем БАГ 1 (order-service проблемы)...
dotnet test --filter "FullyQualifiedName~CartServiceBug1Tests"

echo.
echo 6. Тестируем БАГ 2 (неверный подсчет)...
dotnet test --filter "FullyQualifiedName~CartServiceBug2Tests"

echo.
echo 7. Интеграционные тесты...
dotnet test --filter "FullyQualifiedName~CartServiceIntegrationTests"

echo.
echo Все тесты завершены
pause