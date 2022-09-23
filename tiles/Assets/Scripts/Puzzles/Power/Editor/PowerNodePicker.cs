using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tiles.Puzzles.Power.Editor
{
    public class PowerNodePicker : EditorWindow
    {
        private static class Styles
        {
            public const float Alpha = 0.2f;
            public const float LineWidth = 1f;
            public static readonly Color LineColor = new Color(1, 1, 1, Alpha);
            public const float NodeWidth = 16f;

            public static readonly GUIContent NodeIconHover = EditorGUIUtility.IconContent("DotFill");
            public static readonly GUIContent NodeIcon = TintContent(NodeIconHover, new Color(1, 1, 1, Alpha));
            public static readonly GUIContent NodeIconSelected = EditorGUIUtility.IconContent("DotFrame");
            public static readonly GUIContent NodeIconHoverRing = TintContent(NodeIconSelected, new Color(1, 1, 1, 0.5f));

            private static GUIContent TintContent(GUIContent content, Color tint)
            {
                if (!content.image) return content;
                var newContent = new GUIContent(content);
                newContent.image = TintTexture(content.image, tint);
                return newContent;
            }

            private static Texture2D TintTexture(Texture texture, Color tint)
            {
                var newTexture = new Texture2D(
                    texture.width, texture.height, texture.graphicsFormat,
                    UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
                Graphics.CopyTexture(texture, newTexture);
                var pixels = newTexture.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] *= tint;
                newTexture.SetPixels(pixels);
                newTexture.Apply();
                return newTexture;
            }
        }

        public static void Show(PowerNode current, Action<PowerNode> onPicked)
        {
            var window = CreateInstance<PowerNodePicker>();
            window.titleContent = new("Pick Node");
            window.current = current;
            window.onPicked = onPicked;
            window.ShowAuxWindow();
        }

        private PowerNode current;
        private Action<PowerNode> onPicked;

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;

            Rect contentRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true));
            Rect pickerRect = GetPickerRect(contentRect);
            DrawLines(pickerRect);

            foreach (var node in PowerNode.AllNodes)
            {
                var nodeRect = GetNodeRect(node, pickerRect);
                var hover = nodeRect.Contains(evt.mousePosition);

                switch (evt.GetTypeForControl(id))
                {
                    case EventType.Repaint:
                        if (hover || current == node)
                        {
                            if (current == node)
                                GUI.DrawTexture(nodeRect, Styles.NodeIconSelected.image);
                            else GUI.DrawTexture(nodeRect, Styles.NodeIconHoverRing.image);
                            GUI.DrawTexture(nodeRect, Styles.NodeIconHover.image);
                        } else GUI.DrawTexture(nodeRect, Styles.NodeIcon.image);
                        break;

                    case EventType.MouseDown:
                        if (hover)
                        {
                            current = node;
                            onPicked?.Invoke(node);
                        }
                        break;
                }
            }
        }

        private static Rect GetPickerRect(Rect contentRect)
        {
            if (contentRect.width > contentRect.height)
                return new Rect(
                    contentRect.x + 0.5f * (contentRect.width - contentRect.height),
                    contentRect.y,
                    contentRect.height,
                    contentRect.height);
            return new Rect(
                contentRect.x,
                contentRect.y,
                contentRect.width,
                contentRect.width);
        }

        private static Vector2 GetNodePosition(PowerNode node, Rect pickerRect)
        {
            Vector2 nodeOffset = node.CenterOffset;
            nodeOffset.y = -nodeOffset.y;
            Vector2 offset = nodeOffset * new Vector2(pickerRect.width - Styles.NodeWidth, pickerRect.height - Styles.NodeWidth);
            return pickerRect.center + offset;
        }

        private static Rect GetNodeRect(PowerNode node, Rect pickerRect)
        {
            var nodeCenter = GetNodePosition(node, pickerRect);
            return new Rect(
                nodeCenter.x - 0.5f * Styles.NodeWidth,
                nodeCenter.y - 0.5f * Styles.NodeWidth,
                Styles.NodeWidth,
                Styles.NodeWidth);
        }

        private void DrawLines(Rect pickerRect)
        {
            float halfWidth = Styles.LineWidth * 0.5f;

            // Draw horizontal lines
            for (int y = 0; y < PowerNode.GridSize; y++)
            {
                Vector2 left = GetNodePosition(new PowerNode(0, y), pickerRect);
                Vector2 right = GetNodePosition(new PowerNode(PowerNode.GridSize - 1, y), pickerRect);
                Rect line = new Rect(left.x, left.y - halfWidth, right.x - left.x, Styles.LineWidth);
                EditorGUI.DrawRect(line, Styles.LineColor);
            }

            // Draw vertical lines
            for (int x = 0; x < PowerNode.GridSize; x++)
            {
                Vector2 bottom = GetNodePosition(new PowerNode(x, 0), pickerRect);
                Vector2 top = GetNodePosition(new PowerNode(x, PowerNode.GridSize - 1), pickerRect);
                Rect line = new Rect(bottom.x - halfWidth, bottom.y, Styles.LineWidth, top.y - bottom.y);
                EditorGUI.DrawRect(line, Styles.LineColor);
            }
        }
    }
}
