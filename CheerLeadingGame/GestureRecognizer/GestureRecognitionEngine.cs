using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Kinect;

namespace GestureRecognizer
{
    public class GestureRecognitionEngine
    {
        public event EventHandler<GestureEventArgs> GestureRecognized;
        public event EventHandler<GestureEventArgs> GestureNotRecognized;

        // public GestureType GestureType;
        
        // class exercise 4
        public List<GestureType> GestureTypes = new List<GestureType>();

        public void StartRecognize(Skeleton skeleton)
        {
            foreach (GestureType gt in GestureTypes) // for class exercise 4
            {
                switch (gt)
                //switch (this.GestureType) // for class exercise 4
                {
                    case GestureType.HandsClapping:
                        this.MatchHandClappingGesture(skeleton);
                        break;
                    case GestureType.HandsRaisedAboveHead:
                        MatchHandsRaisedAboveHeadGesture(skeleton);
                        break;
                    case GestureType.HandsAtChestLevel:
                        MatchHandsAtChestLevel(skeleton);
                        break;
                    default:
                        break;
                }
            }
        }

        private float GetJointDistance(Joint j1, Joint j2)
        {
            float distanceX = j1.Position.X - j2.Position.X;
            float distanceY = j1.Position.Y - j2.Position.Y;
            float distanceZ = j1.Position.Z - j2.Position.Z;
            return (float)Math.Sqrt(distanceX * distanceX
                + distanceY * distanceY + distanceZ * distanceZ);
        }

        private float prev_dist = 0.0f;
        private float threshold = 0.2f;

        private void MatchHandClappingGesture(Skeleton skeleton)
        {
            if (skeleton == null) return;

            // class exercise 1	
            Joint hr = skeleton.Joints[JointType.HandRight];
            Joint hl = skeleton.Joints[JointType.HandLeft];
            if (hr.TrackingState != JointTrackingState.NotTracked && hl.TrackingState != JointTrackingState.NotTracked)
            {
                float curr_dist = GetJointDistance(hr, hl);                

                if (curr_dist < threshold && prev_dist > threshold)
                {
                    if (GestureRecognized != null) // already existing some subscriber?
                    {
                        this.GestureRecognized(this, new GestureEventArgs(RecognitionResult.Success));
                    }                    
                }

                prev_dist = curr_dist;
            }
        }

        // class exercise 3
        private SkeletonPoint prev_posi_lh = new SkeletonPoint();
        private SkeletonPoint prev_posi_rh = new SkeletonPoint();
        private float hands_up_threshold = 0.1f;

        private void MatchHandsRaisedAboveHeadGesture(Skeleton skeleton)
        {
            if (skeleton == null) return;

            Joint handL = skeleton.Joints[JointType.HandLeft];
            Joint handR = skeleton.Joints[JointType.HandRight];
            Joint head = skeleton.Joints[JointType.Head];

            if (handL.TrackingState != JointTrackingState.NotTracked &&
                handR.TrackingState != JointTrackingState.NotTracked &&
                head.TrackingState != JointTrackingState.NotTracked)
            {
                SkeletonPoint curr_posi_lh = handL.Position;
                SkeletonPoint curr_posi_rh = handR.Position;

                // requires both hands move from "below head" to "above head"
                if ((prev_posi_lh.Y < head.Position.Y && curr_posi_lh.Y > head.Position.Y &&
                     curr_posi_rh.Y > head.Position.Y - hands_up_threshold && curr_posi_rh.Y < head.Position.Y + hands_up_threshold) ||
                    (prev_posi_rh.Y < head.Position.Y && curr_posi_rh.Y > head.Position.Y &&
                     curr_posi_lh.Y > head.Position.Y - hands_up_threshold && curr_posi_lh.Y < head.Position.Y + hands_up_threshold))
                {
                    if (this.GestureRecognized != null)
                        this.GestureRecognized(this, new GestureEventArgs(RecognitionResult.Success));
                }

                prev_posi_lh = handL.Position;
                prev_posi_rh = handR.Position;

            }
        }
        //Both hands at chest level
        private void MatchHandsAtChestLevel(Skeleton skeleton)
        {
            if (skeleton == null) return;

            // class exercise 1	
            Joint hr = skeleton.Joints[JointType.HandRight];
            Joint hl = skeleton.Joints[JointType.HandLeft];
            if (hr.TrackingState != JointTrackingState.NotTracked && hl.TrackingState != JointTrackingState.NotTracked)
            {

                if (hr.Position.Y < skeleton.Joints[JointType.Head].Position.Y && hl.Position.Y < skeleton.Joints[JointType.Head].Position.Y &&
                    hr.Position.Y > skeleton.Joints[JointType.Spine].Position.Y && hl.Position.Y > skeleton.Joints[JointType.Spine].Position.Y)
                {
                    if (GestureRecognized != null) // already existing some subscriber?
                    {
                        this.GestureRecognized(this, new GestureEventArgs(RecognitionResult.Success));
                    }
                }

            }
        }
    }

}
