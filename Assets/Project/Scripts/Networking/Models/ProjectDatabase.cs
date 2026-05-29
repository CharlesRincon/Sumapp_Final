using UnityEngine;

namespace Networking.Models
{
    [CreateAssetMenu(fileName = "ProjectDatabase", menuName = "Networking/Project Database")]
    public class ProjectDatabase : ScriptableObject
    {
        [SerializeField] private ProjectDefinition[] _projects;

        public bool TryGetProject(int projectId, out ProjectDefinition project)
        {
            if (_projects != null)
            {
                for (int i = 0; i < _projects.Length; i++)
                {
                    var candidate = _projects[i];
                    if (candidate != null && candidate.ProjectId == projectId)
                    {
                        project = candidate;
                        return true;
                    }
                }
            }

            project = null;
            return false;
        }
    }
}