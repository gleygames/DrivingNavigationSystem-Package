using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Gley.NavigationSystem.Tests
{
    public class ApiLookupTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void GetNavigationManager_NoManagerInScene_ReturnsNull()
        {
            Assert.That(API.GetNavigationManager(), Is.Null);
        }

        [Test]
        public void GetNavigationManager_ManagerInScene_ReturnsIt()
        {
            NavigationManager manager = new GameObject("Manager").AddComponent<NavigationManager>();

            Assert.That(API.GetNavigationManager(), Is.SameAs(manager));
        }

        [Test]
        public void GetNavigationManager_ManagerOnInactiveObject_ReturnsNull()
        {
            GameObject managerObject = new GameObject("Manager");
            managerObject.AddComponent<NavigationManager>();
            managerObject.SetActive(false);

            Assert.That(API.GetNavigationManager(), Is.Null);
        }
    }
}
