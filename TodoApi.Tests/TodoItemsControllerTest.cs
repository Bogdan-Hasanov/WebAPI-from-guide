using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NUnit.Framework;
using TodoApi.Controllers;
using Moq;
using TodoApi.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TodoApi.Tests
{
    public class TodoItemsControllerTest
    {
        private readonly TodoContext _context;
        public TodoItemsController CreateTodoItemsController => new TodoItemsController(_context);
        public TodoItemsControllerTest()
        {
            var options = new DbContextOptionsBuilder<TodoContext>()
                .UseInMemoryDatabase(databaseName: "TodoDatabase")
                .Options;
            _context = new TodoContext(options);
            _context.TodoItems.Add(new TodoItem() { Id = 1, IsComplete = false, Name = "Wash hands", Secret = "Secret1" });
            _context.TodoItems.Add(new TodoItem() { Id = 2, IsComplete = false, Name = "Walk dog", Secret = "Secret2" });
            _context.SaveChanges();
        }
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task GetTodoItems_Valid_Test()
        {
            // Arrange
            var todoItemsController = CreateTodoItemsController;

            // Act
            var result = await todoItemsController.GetTodoItems();
            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Value.First(dto => dto.Id == 1).Name, Is.EqualTo("Wash hands"));
                Assert.IsInstanceOf<TodoItemDTO>(result.Value.First());
            });
        }

        [Test]
        public async Task GetTodoItemExists_Valid_Test()
        {
            // Arrange
            var todoItemsController = CreateTodoItemsController;
            // Act
            var result = await todoItemsController.GetTodoItem(2);
            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Value.Name, Is.EqualTo("Walk dog"));
                Assert.IsInstanceOf<TodoItemDTO>(result.Value);
            });
        }

        [TestCase(-1)]
        [TestCase(0)]
        public async Task GetTodoItem_Invalid_Test(int id)
        {
            // Arrange
            var todoItemsController = CreateTodoItemsController;
            // Act
            var result = await todoItemsController.GetTodoItem(id);
            // Assert
            Assert.IsInstanceOf<NotFoundResult>(result.Result);
        }

        [Test]
        public async Task UpdateTodoItem_Valid_Test()
        {
            // Arrange
            var todoItemsController = CreateTodoItemsController;
            _context.TodoItems.Add(new TodoItem() { Id = 3, IsComplete = false, Name = "Buy groceries", Secret = "Secret3" });

            // Act
            var result = await todoItemsController.UpdateTodoItem(3,
                new TodoItemDTO() { Id = 3, IsComplete = true, Name = "Buy groceries" });
            // Assert
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task UpdateTodoItem_Invalid_Test()
        {
            // Arrange
            var todoItemsController = CreateTodoItemsController;
            _context.TodoItems.Add(new TodoItem() { Id = 4, IsComplete = false, Name = "Buy dog food", Secret = "Secret4" });

            // Act
            var result1 = await todoItemsController.UpdateTodoItem(4,
                new TodoItemDTO() { Id = 3, IsComplete = true, Name = "Buy groceries" });
            var result2 = await todoItemsController.UpdateTodoItem(0,
                new TodoItemDTO() { Id = 0, IsComplete = true, Name = "Buy groceries" });
            // Assert
            Assert.Multiple(() =>
            {
                Assert.IsInstanceOf<BadRequestResult>(result1);
                Assert.IsInstanceOf<NotFoundResult>(result2);
            });
        }

        // Not working
        [Test]
        public async Task UpdateTodoItem_Invalid_Concurrency_Test()
        {
            // Arrange
            var dbSetTodoItemMock = new Mock<DbSet<TodoItem>>();
            var concurrencyTodoContext = new Mock<TodoContext>();
            var todoItemsController =  new TodoItemsController(concurrencyTodoContext.Object);
            concurrencyTodoContext.Setup(context => context.TodoItems)
                .Returns(dbSetTodoItemMock.Object);
            dbSetTodoItemMock.Setup(dbSet => dbSet.FindAsync(It.IsAny<long>()))
                .Returns(new ValueTask<TodoItem>());
            concurrencyTodoContext.Setup(context => context.TodoItems.FindAsync(It.IsAny<long>()))
                .Returns(new ValueTask<TodoItem>());
            concurrencyTodoContext.Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException());
            // Act
            var result = await todoItemsController.UpdateTodoItem(3,
                new TodoItemDTO() { Id = 3, IsComplete = true, Name = "Buy groceries" });
            
            // Assert
            Assert.Multiple(() =>
            {
                Assert.IsInstanceOf<NotFoundResult>(result);
            });
        }
    }


}

