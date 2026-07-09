using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Crée des personnages low-poly procéduraux à partir de primitives Unity.
    /// Style épuré qui s'accorde avec les tuiles KayKit.
    /// </summary>
    public static class UnitVisuals
    {
        // Violet Phénicie / Bleu Grèce
        private static readonly Color[] PlayerColors = {
            new Color(0.5f, 0.2f, 0.7f),  // Pourpre phénicien
            new Color(0.2f, 0.5f, 0.9f),  // Bleu grec
        };

        private static readonly Color[] PlayerDarkColors = {
            new Color(0.3f, 0.1f, 0.5f),
            new Color(0.1f, 0.3f, 0.6f),
        };

        /// <summary>
        /// Crée un modèle de guerrier low-poly (capsule corps + sphère tête + cylindre lance)
        /// </summary>
        public static GameObject CreateWarrior(Transform parent, int ownerIndex)
        {
            var go = new GameObject("WarriorModel");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0.4f, 0);

            // Corps (capsule)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
            SetColor(body, PlayerColors[ownerIndex]);

            // Tête (sphère)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0, 0.4f, 0);
            head.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            SetColor(head, new Color(0.95f, 0.85f, 0.7f)); // Peau

            // Lance (cylindre fin)
            var spear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spear.name = "Spear";
            spear.transform.SetParent(go.transform, false);
            spear.transform.localPosition = new Vector3(0.2f, 0.3f, 0);
            spear.transform.localRotation = Quaternion.Euler(0, 0, -45);
            spear.transform.localScale = new Vector3(0.03f, 0.6f, 0.03f);
            SetColor(spear, new Color(0.5f, 0.3f, 0.1f)); // Bois

            // Bouclier (cylindre aplati)
            var shield = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shield.name = "Shield";
            shield.transform.SetParent(go.transform, false);
            shield.transform.localPosition = new Vector3(-0.15f, 0.1f, 0);
            shield.transform.localScale = new Vector3(0.2f, 0.02f, 0.2f);
            SetColor(shield, PlayerDarkColors[ownerIndex]);

            return go;
        }

        /// <summary>
        /// Crée un éclaireur (plus petit, juste une capsule + tête)
        /// </summary>
        public static GameObject CreateScout(Transform parent, int ownerIndex)
        {
            var go = new GameObject("ScoutModel");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0.3f, 0);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(0.2f, 0.25f, 0.2f);
            SetColor(body, PlayerColors[ownerIndex] * 0.8f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0, 0.3f, 0);
            head.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            SetColor(head, new Color(0.95f, 0.85f, 0.7f));

            return go;
        }

        /// <summary>
        /// Crée un cavalier (cheval + personnage)
        /// </summary>
        public static GameObject CreateCavalry(Transform parent, int ownerIndex)
        {
            var go = new GameObject("CavalryModel");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0.4f, 0);

            // Cheval (2 capsules)
            var horseBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            horseBody.name = "HorseBody";
            horseBody.transform.SetParent(go.transform, false);
            horseBody.transform.localPosition = Vector3.zero;
            horseBody.transform.localRotation = Quaternion.Euler(0, 0, 90);
            horseBody.transform.localScale = new Vector3(0.3f, 0.5f, 0.25f);
            SetColor(horseBody, new Color(0.4f, 0.25f, 0.1f));

            // Tête du cheval
            var horseHead = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            horseHead.name = "HorseHead";
            horseHead.transform.SetParent(go.transform, false);
            horseHead.transform.localPosition = new Vector3(0.4f, 0.15f, 0);
            horseHead.transform.localScale = new Vector3(0.15f, 0.2f, 0.15f);
            SetColor(horseHead, new Color(0.35f, 0.2f, 0.08f));

            // Cavalier
            var rider = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rider.name = "Rider";
            rider.transform.SetParent(go.transform, false);
            rider.transform.localPosition = new Vector3(0, 0.35f, 0);
            rider.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
            SetColor(rider, PlayerColors[ownerIndex]);

            var riderHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            riderHead.transform.SetParent(go.transform, false);
            riderHead.transform.localPosition = new Vector3(0, 0.6f, 0);
            riderHead.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            SetColor(riderHead, new Color(0.95f, 0.85f, 0.7f));

            return go;
        }

        /// <summary>
        /// Crée un bateau (coque + voile)
        /// </summary>
        public static GameObject CreateShip(Transform parent, int ownerIndex)
        {
            var go = new GameObject("ShipModel");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0.3f, 0);

            // Coque (cube allongé)
            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull";
            hull.transform.SetParent(go.transform, false);
            hull.transform.localScale = new Vector3(0.3f, 0.12f, 0.6f);
            SetColor(hull, new Color(0.5f, 0.3f, 0.1f));

            // Voile (cube aplati)
            var sail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sail.name = "Sail";
            sail.transform.SetParent(go.transform, false);
            sail.transform.localPosition = new Vector3(0, 0.3f, 0);
            sail.transform.localScale = new Vector3(0.02f, 0.3f, 0.25f);
            SetColor(sail, PlayerColors[ownerIndex]);

            // Mât
            var mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Mast";
            mast.transform.SetParent(go.transform, false);
            mast.transform.localPosition = new Vector3(0, 0.2f, 0);
            mast.transform.localScale = new Vector3(0.02f, 0.35f, 0.02f);
            SetColor(mast, new Color(0.4f, 0.25f, 0.1f));

            return go;
        }

        private static void SetColor(GameObject go, Color color)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mr.sharedMaterial = mat;
        }
    }
}
