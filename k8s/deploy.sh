#!/bin/bash

#Faz o replace das versoes nos deployments do kube
#replace_version()
# {
#        cd ./deployment
#        while read line
#        do
#                line2="$(echo "${line::-5}" | awk '{print toupper($0)}')"
#                echo $line2
#                version="GO_DEPENDENCY_LABEL_REDES_${line2//-/_}_BUILD"
#                sed -i "s/$version/${!version}/" $line
#        done < <(ls *.yaml)
# }

#Faz o deploy do k8s
deploy_k8s()
{
        #Verifica se o namespace existe, caso não exista, cria.
        {
                kubectl get namespace ${NAMESPACE}
        } || {
                kubectl create -f ./namespace/${NAMESPACE}.yaml
        }
        #Tenta o replace do configmap, caso nao exista, cria.
        {
                kubectl create configmap $CONFIGMAP --namespace $NAMESPACE --from-env-file ./configmap/$AMBIENTE.env -o yaml --dry-run |  kubectl replace -f -
        } || {
                kubectl create configmap $CONFIGMAP  --namespace $NAMESPACE --from-env-file ./configmap/$AMBIENTE.env
        }
        #Tenta o replace do secrets, caso nao exista cria.
        {
                kubectl create secret generic ${NAMESPACE}-secrets --namespace ${NAMESPACE} --from-env-file ./secret/pwd.secret -o yaml --dry-run |  kubectl replace -f -
        } || {
                kubectl create secret generic ${NAMESPACE}-secrets --namespace ${NAMESPACE} --from-env-file ./secret/pwd.secret
        }

        #Verifica se o deployment existe. Caso não exista, cria.
        cd ./deployment
        while read line
        do
                sed -i "s/#{REPLICAS_APIS}/${REPLICAS_APIS}/" $line
                sed -i "s/#{REPLICAS_SERVICES}/${REPLICAS_SERVICES}/" $line
                sed -i "s/#{DOCKER_REGISTRY}/${DOCKER_REGISTRY}/" $line
                {
                        kubectl create -f $line -o yaml --dry-run | kubectl replace -f -
                } || {
                        kubectl create -f $line
                }
        done < <(ls *.yaml)        
}

$1