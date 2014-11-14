using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Brumba.DsspUtils;
using Microsoft.Ccr.Core;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Dss.Services.Serializer;
using Mrse = Microsoft.Robotics.Simulation.Engine;
using MrsePxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using MrsPxy = Microsoft.Robotics.Simulation.Proxy;

namespace Brumba.SimulationTester
{
    public class EntityDeserializer
    {
        private readonly SerializerPort _serializerPort;
        private readonly Action<Exception> _logError;

        public EntityDeserializer(SerializerPort serializerPort, Action<Exception> logError)
        {
            _serializerPort = serializerPort;
            _logError = logError;
        }

        public IEnumerator<ITask> DeserializeTopLevelEntityPxies(Action<IEnumerable<MrsePxy.VisualEntity>> @return,
            MrsPxy.ObjectList serializedEntities, Func<XmlElement, bool> filter)
        {
            var entities = new List<MrsePxy.VisualEntity>();
            foreach (var entityNode in serializedEntities.XmlNodes.Cast<XmlElement>().Where(filter ?? (xe => true)))
            {
                MrsePxy.VisualEntity entityPxy = null;
                yield return To.Exec(DeserializeEntityPxyFromXml, (MrsePxy.VisualEntity e) => entityPxy = e, entityNode);
                //if (entityPxy.State.Name == "MainCamera")
                //    continue;
                entities.Add(entityPxy);
            }
            @return(entities);
        }

        public IEnumerator<ITask> DeserializeTopLevelEntities(Action<IEnumerable<Mrse.VisualEntity>> @return,
            MrsPxy.ObjectList serializedEntities)
        {
            IEnumerable<MrsePxy.VisualEntity> entitiesFlatPxies = null;
            yield return To.Exec(DeserializeTopLevelEntityPxies, (IEnumerable<MrsePxy.VisualEntity> es) => entitiesFlatPxies = es,
                    serializedEntities, (Func<XmlElement, bool>)null);

            var entitiesFlat = entitiesFlatPxies.Select(ePxy => (Mrse.VisualEntity) DssTypeHelper.TransformFromProxy(ePxy));

            var entitiesTop = new List<Mrse.VisualEntity>();
            while (entitiesFlat.Any())
            {
                entitiesTop.Add(entitiesFlat.First());
                entitiesFlat = ReuniteEntity(entitiesFlat.First(), entitiesFlat.Skip(1));
            }
            @return(entitiesTop);
        }

        private IEnumerable<Mrse.VisualEntity> ReuniteEntity(Mrse.VisualEntity parent,
            IEnumerable<Mrse.VisualEntity> entitiesFlat)
        {
            if (parent.ChildCount != 0)
                for (var i = 0; i < parent.ChildCount; ++i)
                {
                    parent.InsertEntityGlobal(entitiesFlat.First());
                    entitiesFlat = ReuniteEntity(entitiesFlat.First(), entitiesFlat.Skip(1));
                }
            return entitiesFlat.ToList();
        }

        private IEnumerator<ITask> DeserializeEntityPxyFromXml(Action<MrsePxy.VisualEntity> @return,
            XmlElement entityNode)
        {
            var desRequest = new Deserialize(new XmlNodeReader(entityNode));
            _serializerPort.Post(desRequest);
            DeserializeResult desEntity = null;
            yield return Arbiter.Choice(desRequest.ResultPort, v => desEntity = v, e => _logError(e));
            @return((MrsePxy.VisualEntity) desEntity.Instance);
        }
    }
}