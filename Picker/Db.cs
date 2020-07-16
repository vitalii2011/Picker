using UnityEngine;

namespace Picker
{
    public static class Db
    {
        public static bool on = false;

        public static void l(object m)
        {
            if (on) Debug.Log(m);
        }

        public static void w(object m)
        {
            if (on) Debug.LogWarning(m);
        }

        public static void e(object m)
        {
            if (on) Debug.LogWarning(m);
        }

        // Extensions
        public static NetSegment S(this ushort s)
        {
            //Debug.Log(s);
            return NetManager.instance.m_segments.m_buffer[s];
        }

        public static NetNode N(this ushort n)
        {
            //Debug.Log(n);
            return NetManager.instance.m_nodes.m_buffer[n];
        }

        public static PropInstance P(this ushort p)
        {
            //Debug.Log(p);
            return PropManager.instance.m_props.m_buffer[p];
        }

        public static Building B(this ushort b)
        {
            //Debug.Log(b);
            Building building = BuildingManager.instance.m_buildings.m_buffer[b];
            while (building.m_parentBuilding > 0)
            {
                building = BuildingManager.instance.m_buildings.m_buffer[building.m_parentBuilding];
            }
            return building;
        }

        public static TreeInstance T(this uint t)
        {
            //Debug.Log(t);
            return TreeManager.instance.m_trees.m_buffer[t];
        }

        public static Vector3 Position(this InstanceID id)
        {
            if (id.NetSegment != 0) return id.NetSegment.S().m_middlePosition;
            if (id.NetNode != 0 && id.NetNode < 32768) return id.NetNode.N().m_position;
            if (id.Prop != 0) return id.Prop.P().Position;
            if (id.Building != 0) return id.Building.B().m_position;
            if (id.Tree != 0) return id.Tree.T().Position;

            return Vector3.zero;
        }

        public static PrefabInfo Info(this InstanceID id)
        {
            if (id.NetSegment != 0) return id.NetSegment.S().Info;
            if (id.NetNode != 0 && id.NetNode < 32768) return id.NetNode.N().Info;
            if (id.Prop != 0) return id.Prop.P().Info;
            if (id.Building != 0) return id.Building.B().Info;
            if (id.Tree != 0) return id.Tree.T().Info;

            return null;
        }
    }
}
