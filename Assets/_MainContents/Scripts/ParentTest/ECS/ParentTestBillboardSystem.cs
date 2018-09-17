﻿using UnityEngine;
using UnityEngine.Assertions;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Burst;
using Unity.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using System;

using MainContents.Billboard.ECS;

namespace MainContents.ParentTest.ECS
{
    /// <summary>
    /// ドカベンロゴビルボードシステム ※JobSystem併用
    /// </summary>
    public class ParentTestBillboardJobSystem : JobComponentSystem
    {
        /// <summary>
        /// カメラ情報参照用Entity
        /// </summary>
        struct SharedCameraDataGroup
        {
            public readonly int Length;
            [ReadOnly] public ComponentDataArray<SharedCameraData> Dummy;   // これはInject用の識別子みたいなもの
            [ReadOnly] public SharedComponentDataArray<CameraRotation> CameraRotation;
        }

        /// <summary>
        /// ビルボード用Job
        /// </summary>
        [BurstCompile]
        struct BillboardJob : IJobProcessComponentData<Rotation>
        {
            public quaternion CameraRotation;
            public void Execute(ref Rotation rot)
            {
                rot.Value = this.CameraRotation;
            }
        }

        [Inject] SharedCameraDataGroup _sharedCameraDataGroup;
        BillboardJob _billboardJob;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            this._billboardJob = new BillboardJob();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // 1個しか無い想定
            Assert.IsTrue(this._sharedCameraDataGroup.Length == 1);

            // IJobProcessComponentDataに対しISharedComponentDataを直接渡すことは出来ない?みたいなので、
            // 予めInjectしたカメラの回転情報をScheduleを叩く前に渡した上で実行する
            this._billboardJob.CameraRotation = this._sharedCameraDataGroup.CameraRotation[0].Value;
            return this._billboardJob.Schedule(this, inputDeps);
        }
    }

    /// <summary>
    /// ドカベンロゴビルボードシステム
    /// </summary>
    public class ParentTestBillboardSystem : ComponentSystem
    {
        struct SharedCameraDataGroup
        {
            public readonly int Length;
            [ReadOnly] public ComponentDataArray<SharedCameraData> Dummy;
            [ReadOnly] public SharedComponentDataArray<CameraRotation> CameraRotation;
        }

        // ビルボード用ルートノード
        struct RootGroup
        {
            public readonly int Length;
            public ComponentDataArray<Rotation> Rotation;
        }

        [Inject] SharedCameraDataGroup _sharedCameraDataGroup;
        [Inject] RootGroup _rootGroup;

        protected override void OnUpdate()
        {
            Assert.IsTrue(this._sharedCameraDataGroup.Length == 1);
            var cameraRot = this._sharedCameraDataGroup.CameraRotation[0].Value;
            for (int i = 0; i < this._rootGroup.Length; ++i)
            {
                var rot = this._rootGroup.Rotation[i];
                rot.Value = cameraRot;
                this._rootGroup.Rotation[i] = rot;
            }
        }
    }
}