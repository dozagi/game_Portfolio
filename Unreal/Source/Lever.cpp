// Fill out your copyright notice in the Description page of Project Settings.

#include "Lever.h"
#include "Components/StaticMeshComponent.h"
#include "UObject/ConstructorHelpers.h"					
#include	"MyCharacter/MotionControllerCharacter.h"	
#include "Kismet/KismetMathLibrary.h"
#include "Kismet/GameplayStatics.h"							
#include "HandMotionController/RightHandMotionController.h"
#include "HandMotionController/LeftHandMotionController.h"
#include "Engine/World.h"
#include "Engine/StaticMesh.h"

#include"Components/BoxComponent.h"
#include"Components/StaticMeshComponent.h"
// Sets default values

// 캐릭터의 손을 문 Transform기준으로 바꾸어 Atan2에서 각을 찾고 Rotation에 적용합니다. 
// 밑변과 높이를 알 때 라디안=atan(높이/밑변)이기 때문에 각도를 얻을 수 있습니다.

ALever::ALever()
{
	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	Scene = CreateDefaultSubobject<USceneComponent>(TEXT("Scene"));
	Scene->SetupAttachment(RootComponent);

	LeverScene = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("LeverScene"));
	LeverScene->SetupAttachment(Scene);

	Lever = CreateDefaultSubobject<UBoxComponent>(TEXT("Lever"));
	Lever->SetupAttachment(LeverScene);

	Collision = CreateDefaultSubobject<UBoxComponent>(TEXT("Collision"));
	Collision->SetupAttachment(Lever);

	static ConstructorHelpers::FObjectFinder<UStaticMesh>SM_Door(TEXT("StaticMesh'/Game/Assets/MapBuild/RoughMap/Cathedral_door/backup/Main_door.Main_door'"));	
	if (SM_Door.Succeeded())		
	{
		LeverScene->SetStaticMesh(SM_Door.Object);		
	}

	Scene->SetRelativeScale3D(FVector(0.117701f, 0.117701f, 0.117701f));

	LeverScene->SetRelativeScale3D(FVector(4.0f, 4.0f, 4.0f));

	Lever->SetRelativeLocation(FVector(-99.801483f, 2.628052f, 191.0f));
	Lever->SetRelativeScale3D(FVector(0.318f, 0.787583f, 1.380393f));

	Collision->SetRelativeLocation(FVector(148.561737f, 0.259847f, 15.835767f));
	Collision->SetRelativeScale3D(FVector(4.620687f, 0.188942f, 3.429634f));

	AutoRot = FRotator(LeverScene->RelativeRotation.Pitch, LeverScene->RelativeRotation.Yaw + 85.0f, LeverScene->RelativeRotation.Roll);
	DefaultYaw = LeverScene->GetComponentRotation().Yaw;

	Collision->bGenerateOverlapEvents = false;

	// 각 컴포넌트의 콜리전의 속성을 설정합니다.  
	LeverScene->SetCollisionProfileName("NoCollision");		
	Lever->SetCollisionProfileName("OverlapAll");				
	Collision->SetCollisionProfileName("BlockAll");				

	Tags.Add(FName("Door"));
}

// Called when the game starts or when spawned
void ALever::BeginPlay()
{
	Super::BeginPlay();

	Lever->OnComponentBeginOverlap.AddDynamic(this, &ALever::OnLeverOverlap);	
	Lever->OnComponentEndOverlap.AddDynamic(this, &ALever::OnLeverEndOverlap);
}

// Called every frame
void ALever::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	// 문을 잡고 있을 때 열수가 있습니다. 
	if (TouchActor)
	{
		ARightHandMotionController* RightHand = Cast<ARightHandMotionController>(TouchActor);
		ALeftHandMotionController* LeftHand = Cast<ALeftHandMotionController>(TouchActor);
		if (RightHand)
		{
			if (RightHand->bisRightGrab)
			{
				// 손의 위치를 문의 Transform으로 변환 후, 손의 위치에 따라 문이 열리게 합니다.
				FVector Cal = UKismetMathLibrary::InverseTransformLocation
				(GetActorTransform(), RightHand->GetActorLocation());

				float degree = UKismetMathLibrary::RadiansToDegrees(UKismetMathLibrary::Atan2(-Cal.Y, -Cal.X));

				// 각도 적용
				if (degree > 0.0f)
					LeverScene->SetRelativeRotation(FRotator(0.0f, degree, 0.0f));
			}
		}
		else if (LeftHand)
		{
			if (LeftHand->bisLeftGrab)
			{
				// 손의 위치를 문의 Transform으로 변환 후, 손의 위치에 따라 문이 열리게 합니다.
				FVector Cal = UKismetMathLibrary::InverseTransformLocation
				(GetActorTransform(), LeftHand->GetActorLocation());

				float degree = UKismetMathLibrary::RadiansToDegrees(UKismetMathLibrary::Atan2(-Cal.Y, -Cal.X));

				// 각도 적용
				if (degree > 0.0f)
					LeverScene->SetRelativeRotation(FRotator(0.0f, degree, 0.0f));
			}
		}
	}

	// 각도가 10이상일 경우 자동적으로 열립니다.
	if (LeverScene->RelativeRotation.Yaw > 10.0f)
	{
		LeverScene->SetRelativeRotation(FMath::Lerp(LeverScene->RelativeRotation, AutoRot, 0.01f));
	}

}

// 오버랩된 액터가 손일 경우 TouchActor에 값이 들어옵니다. (손 만 값이 들어옵니다.)
void ALever::OnLeverOverlap(UPrimitiveComponent * OverlappedComp, AActor * OtherActor, UPrimitiveComponent * OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult & SweepResult)
{
	if (OtherActor->ActorHasTag("RightHand"))
	{
		AMotionControllerCharacter* Character = Cast<AMotionControllerCharacter>(UGameplayStatics::GetPlayerCharacter(GetWorld(), 0));
		if (Character)
		{
			ARightHandMotionController* RightHand = Cast<ARightHandMotionController>(OtherActor);

			if (RightHand)
				TouchActor = Character->RightHand;
		}
	}

	if (OtherActor->ActorHasTag("LeftHand"))
	{
		AMotionControllerCharacter* Character = Cast<AMotionControllerCharacter>(UGameplayStatics::GetPlayerCharacter(GetWorld(), 0));
		if (Character)
		{
			ALeftHandMotionController* RightHand = Cast<ALeftHandMotionController>(OtherActor);

			if (RightHand)
				TouchActor = Character->RightHand;
		}
	}
}

void ALever::OnLeverEndOverlap(UPrimitiveComponent * OverlappedComponent, AActor * OtherActor, UPrimitiveComponent * OtherComp, int32 OtherBodyIndex)
{

}

